using MongoDB.Driver;
using CornerShop.Models;
using MongoDB.Bson;

namespace CornerShop.Services
{
    public class MongoDatabaseService : IDatabaseService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Product> _products;
        private readonly IMongoCollection<Sale> _sales;
        private readonly IMongoCollection<Store> _stores;

        public MongoDatabaseService(string connectionString = "mongodb://localhost:27017", string databaseName = "cornerShop")
        {
            try
            {
                var client = new MongoClient(connectionString);
                _database = client.GetDatabase(databaseName);
                _products = _database.GetCollection<Product>("products");
                _sales = _database.GetCollection<Sale>("sales");
                _stores = _database.GetCollection<Store>("stores");
                Console.WriteLine("Successfully connected to MongoDB!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to MongoDB: {ex.Message}");
                throw;
            }
        }

        public MongoDatabaseService(IMongoDatabase database)
        {
            _database = database;
            _products = _database.GetCollection<Product>("products");
            _sales = _database.GetCollection<Sale>("sales");
            _stores = _database.GetCollection<Store>("stores");
        }

        public async Task InitializeDatabase()
        {
            // Create indexes for products
            var productIndexKeys = Builders<Product>.IndexKeys.Ascending(p => p.Name);
            var productIndexModel = new CreateIndexModel<Product>(productIndexKeys);
            await _products.Indexes.CreateOneAsync(productIndexModel);

            // Create compound index for store-specific products
            var storeProductIndexKeys = Builders<Product>.IndexKeys.Combine(
                Builders<Product>.IndexKeys.Ascending(p => p.StoreId),
                Builders<Product>.IndexKeys.Ascending(p => p.Name)
            );
            var storeProductIndexModel = new CreateIndexModel<Product>(storeProductIndexKeys);
            await _products.Indexes.CreateOneAsync(storeProductIndexModel);

            // Create indexes for sales
            var saleIndexKeys = Builders<Sale>.IndexKeys.Combine(
                Builders<Sale>.IndexKeys.Ascending(s => s.StoreId),
                Builders<Sale>.IndexKeys.Ascending(s => s.Date)
            );
            var saleIndexModel = new CreateIndexModel<Sale>(saleIndexKeys);
            await _sales.Indexes.CreateOneAsync(saleIndexModel);

            // Create indexes for stores
            var storeIndexKeys = Builders<Store>.IndexKeys.Ascending(s => s.Name);
            var storeIndexModel = new CreateIndexModel<Store>(storeIndexKeys);
            await _stores.Indexes.CreateOneAsync(storeIndexModel);
        }

        public async Task<List<Product>> SearchProducts(string searchTerm, string? storeId = null)
        {
            var filter = storeId != null
                ? Builders<Product>.Filter.And(
                    Builders<Product>.Filter.Eq(p => p.StoreId, storeId),
                    Builders<Product>.Filter.Regex(p => p.Name, new BsonRegularExpression(searchTerm, "i")))
                : Builders<Product>.Filter.Regex(p => p.Name, new BsonRegularExpression(searchTerm, "i"));

            var cursor = await _products.FindAsync(filter);
            return await cursor.ToListAsync();
        }

        public async Task<Product?> GetProductByName(string name, string storeId)
        {
            var filter = Builders<Product>.Filter.And(
                Builders<Product>.Filter.Eq(p => p.Name, name),
                Builders<Product>.Filter.Eq(p => p.StoreId, storeId));
            return await _products.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateProductStock(string productName, string storeId, int quantity)
        {
            var filter = Builders<Product>.Filter.And(
                Builders<Product>.Filter.Eq(p => p.Name, productName),
                Builders<Product>.Filter.Eq(p => p.StoreId, storeId));
            var update = Builders<Product>.Update
                .Inc(p => p.StockQuantity, -quantity)
                .Set(p => p.LastUpdated, DateTime.UtcNow);
            var result = await _products.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<string> CreateSale(Sale sale)
        {
            sale.Date = DateTime.UtcNow;
            sale.SyncStatus = "Pending";
            await _sales.InsertOneAsync(sale);
            return sale.Id;
        }

        public async Task<List<Sale>> GetRecentSales(string storeId, int limit = 10)
        {
            var filter = Builders<Sale>.Filter.Eq(s => s.StoreId, storeId);
            var sort = Builders<Sale>.Sort.Descending(s => s.Date);
            return await _sales.Find(filter).Sort(sort).Limit(limit).ToListAsync();
        }

        public async Task<Sale?> GetSaleById(string saleId)
        {
            var filter = Builders<Sale>.Filter.Eq(s => s.Id, saleId);
            return await _sales.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> CancelSale(string saleId)
        {
            var filter = Builders<Sale>.Filter.Eq(s => s.Id, saleId);
            var sale = await _sales.Find(filter).FirstOrDefaultAsync();

            if (sale == null) return false;

            // Restore stock for each item
            foreach (var item in sale.Items)
            {
                await UpdateProductStock(item.ProductName, sale.StoreId, -item.Quantity);
            }

            var update = Builders<Sale>.Update
                .Set(s => s.Status, "Cancelled")
                .Set(s => s.SyncStatus, "Pending");
            var updateResult = await _sales.UpdateOneAsync(filter, update);
            return updateResult.ModifiedCount > 0;
        }

        public async Task<List<Product>> GetAllProducts(string? storeId = null)
        {
            var filter = storeId != null
                ? Builders<Product>.Filter.Eq(p => p.StoreId, storeId)
                : Builders<Product>.Filter.Empty;
            return await _products.Find(filter).ToListAsync();
        }

        public async Task CreateProduct(Product product)
        {
            product.LastUpdated = DateTime.UtcNow;
            await _products.InsertOneAsync(product);
        }

        public async Task<Dictionary<string, object>> GetConsolidatedReport(DateTime startDate, DateTime endDate)
        {
            var report = new Dictionary<string, object>();

            // Get all stores
            var stores = await _stores.Find(_ => true).ToListAsync();
            report["TotalStores"] = stores.Count;

            // Get total sales across all stores
            var salesFilter = Builders<Sale>.Filter.And(
                Builders<Sale>.Filter.Gte(s => s.Date, startDate),
                Builders<Sale>.Filter.Lte(s => s.Date, endDate)
            );
            var sales = await _sales.Find(salesFilter).ToListAsync();

            report["TotalSales"] = sales.Count;
            report["TotalRevenue"] = sales.Sum(s => s.TotalAmount);

            // Get store-specific statistics
            var storeStats = new List<Dictionary<string, object>>();
            foreach (var store in stores)
            {
                var storeSales = sales.Where(s => s.StoreId == store.Id).ToList();
                var storeStat = new Dictionary<string, object>
                {
                    ["StoreId"] = store.Id,
                    ["StoreName"] = store.Name,
                    ["SalesCount"] = storeSales.Count,
                    ["Revenue"] = storeSales.Sum(s => s.TotalAmount)
                };
                storeStats.Add(storeStat);
            }
            report["StoreStatistics"] = storeStats;

            return report;
        }

        public async Task<List<Sale>> GetAllSales()
        {
            return await _sales.Find(Builders<Sale>.Filter.Empty).ToListAsync();
        }

        public async Task<Product?> GetProductById(string id)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.Id, id);
            return await _products.Find(filter).FirstOrDefaultAsync();
        }

        public async Task DeleteProduct(string id, string storeId)
        {
            var filter = Builders<Product>.Filter.And(
                Builders<Product>.Filter.Eq(p => p.Id, id),
                Builders<Product>.Filter.Eq(p => p.StoreId, storeId));
            await _products.DeleteOneAsync(filter);
        }

        public async Task UpdateProduct(Product product)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
            await _products.ReplaceOneAsync(filter, product);
        }

        public async Task<bool> UpdateSale(Sale sale)
        {
            var filter = Builders<Sale>.Filter.Eq(s => s.Id, sale.Id);
            var result = await _sales.ReplaceOneAsync(filter, sale);
            return result.ModifiedCount > 0;
        }
    }
}
