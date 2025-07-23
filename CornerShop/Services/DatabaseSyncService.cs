using CornerShop.Models;
using MongoDB.Bson;

namespace CornerShop.Services
{
    public class DatabaseSyncService
    {
        private readonly IDatabaseService _mongoDb;
        private readonly IDatabaseService _efDb;

        public DatabaseSyncService(IDatabaseService mongoDb, IDatabaseService efDb)
        {
            _mongoDb = mongoDb;
            _efDb = efDb;
        }

        public async Task SyncDatabases()
        {
            // Sync products
            var mongoProducts = await _mongoDb.GetAllProducts();
            var efProducts = await _efDb.GetAllProducts();

            // Sync from MongoDB to EF Core
            foreach (var product in mongoProducts)
            {
                var efProduct = efProducts.FirstOrDefault(p => p.Name == product.Name);
                if (efProduct == null)
                {
                    // Create new product with MongoDB ID
                    var newProduct = new Product
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Category = product.Category,
                        Price = product.Price,
                        StockQuantity = product.StockQuantity
                    };
                    await _efDb.CreateProduct(newProduct);
                }
                else if (efProduct.StockQuantity != product.StockQuantity)
                {
                    await _efDb.UpdateProductStock(product.Name, product.StoreId, product.StockQuantity - efProduct.StockQuantity);
                }
            }

            // Sync from EF Core to MongoDB
            foreach (var product in efProducts)
            {
                var mongoProduct = mongoProducts.FirstOrDefault(p => p.Name == product.Name);
                if (mongoProduct == null)
                {
                    // Create new product with new MongoDB ID
                    var newProduct = new Product
                    {
                        Name = product.Name,
                        Category = product.Category,
                        Price = product.Price,
                        StockQuantity = product.StockQuantity
                    };
                    await _mongoDb.CreateProduct(newProduct);
                }
                else if (mongoProduct.StockQuantity != product.StockQuantity)
                {
                    await _mongoDb.UpdateProductStock(product.Name, product.StoreId, product.StockQuantity - mongoProduct.StockQuantity);
                }
            }

            // Sync sales
            var thirtyDaysAgo = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            var mongoSales = await _mongoDb.GetRecentSales(thirtyDaysAgo);
            var efSales = await _efDb.GetRecentSales(thirtyDaysAgo);

            // Sync from MongoDB to EF Core
            foreach (var sale in mongoSales)
            {
                var efSale = efSales.FirstOrDefault(s => s.Id == sale.Id);
                if (efSale == null)
                {
                    await _efDb.CreateSale(sale);
                }
            }

            // Sync from EF Core to MongoDB
            foreach (var sale in efSales)
            {
                var mongoSale = mongoSales.FirstOrDefault(s => s.Id == sale.Id);
                if (mongoSale == null)
                {
                    await _mongoDb.CreateSale(sale);
                }
            }
        }
    }
}
