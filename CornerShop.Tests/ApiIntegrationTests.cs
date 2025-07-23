using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CornerShop.Services;
using CornerShop.Models;
using Xunit;
using Microsoft.AspNetCore.Hosting;

namespace CornerShop.Tests;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var loginRequest = new { Username = "admin", Password = "password" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new { Username = "admin", Password = "wrongpassword" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithoutAuthentication_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<Product>>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Links);
    }

    [Fact]
    public async Task GetProducts_WithPaginationAndSorting_ReturnsCorrectResults()
    {
        // Arrange
        var url = "/api/v1/products?page=1&pageSize=5&sortBy=Name&sortOrder=asc";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<Product>>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Count() <= 5); // Page size limit
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        var url = "/api/v1/products?searchTerm=test";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<Product>>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CreateProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Category = "Test Category",
            Price = 10.99m,
            StockQuantity = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", product);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithAuthentication_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthToken();
        var product = new Product
        {
            Name = "Test Product",
            Category = "Test Category",
            Price = 10.99m,
            StockQuantity = 100
        };

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", product);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Product>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal(product.Name, result.Data.Name);
    }

    [Fact]
    public async Task GetStores_WithoutAuthentication_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/stores");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<Store>>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Links);
    }

    [Fact]
    public async Task GetStores_WithPaginationAndSorting_ReturnsCorrectResults()
    {
        // Arrange
        var url = "/api/v1/stores?page=1&pageSize=3&sortBy=Name&sortOrder=asc";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<Store>>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Count() <= 3); // Page size limit
    }

    [Fact]
    public async Task CreateStore_WithAuthentication_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthToken();
        var store = new Store
        {
            Name = "Test Store",
            Location = "Test Location",
            Address = "123 Test Street"
        };

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/stores", store);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Store>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal(store.Name, result.Data.Name);
    }

    [Fact]
    public async Task GetSales_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/sales/store/test-store/recent");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSales_WithAuthentication_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Ensure the store with ID 'test-store' exists
        await EnsureStoreExists("test-store", token);

        // Act
        var response = await _client.GetAsync("/api/v1/sales/store/test-store/recent");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<Sale>>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetReports_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/reports/sales/consolidated");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReports_WithAuthentication_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/sales/consolidated");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ConsolidatedSalesReport>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task SearchProducts_WithEmptySearchTerm_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products/search?searchTerm=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(result);
        Assert.Equal(400, result.Status);
        Assert.Contains("Search term is required", result.Message);
    }

    [Fact]
    public async Task GetProduct_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products/invalid-id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(result);
        Assert.Equal(404, result.Status);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task UpdateProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var product = new Product
        {
            Name = "Updated Product",
            Category = "Updated Category",
            Price = 15.99m
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/products/test-id", product);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync("/api/v1/products/test-id");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSalesByDateRange_WithAuthentication_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;
        var url = $"/api/v1/sales/date-range?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&page=1&pageSize=10&sortBy=Date&sortOrder=desc";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<Sale>>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    private async Task<string> GetAuthToken()
    {
        var loginRequest = new { Username = "admin", Password = "password" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("token").GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private async Task EnsureStoreExists(string storeId, string token)
    {
        var store = new Store
        {
            Id = storeId,
            Name = "Test Store",
            Location = "Test Location",
            Address = "123 Test Street"
        };
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync("/api/v1/stores", store);
        // Ignore if already exists or created
    }
}

// API Response Models for testing
public class ApiResponse<T>
{
    public T Data { get; set; } = default!;
    public List<Link> Links { get; set; } = new();
}

public class Link
{
    public string Href { get; set; } = string.Empty;
    public string Rel { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public DateTime Timestamp { get; set; }
    public int Status { get; set; }
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public class ConsolidatedSalesReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSaleAmount { get; set; }
    public List<StoreSalesReport> StoreReports { get; set; } = new();
}

public class StoreSalesReport
{
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSaleAmount { get; set; }
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:MongoDB"] = "mongodb://localhost:27017",
                ["ConnectionStrings:SQLite"] = "Data Source=:memory:",
                ["UseInMemoryDatabase"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all real service registrations
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(IStoreService) ||
                d.ServiceType == typeof(IProductService) ||
                d.ServiceType == typeof(ISaleService) ||
                d.ServiceType == typeof(IDatabaseService)).ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            // Add in-memory services for testing
            services.AddSingleton<IProductService, InMemoryProductService>();
            services.AddSingleton<IStoreService, InMemoryStoreService>();
            services.AddSingleton<ISaleService, InMemorySaleService>();
            services.AddSingleton<IDatabaseService, InMemoryDatabaseService>();
        });
    }
}

// In-memory service implementations for testing
public class InMemoryProductService : IProductService
{
    private readonly List<Product> _products = new();

    public async Task<List<Product>> SearchProducts(string searchTerm, string? storeId = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

        var query = _products.AsQueryable();

        if (!string.IsNullOrEmpty(storeId))
        {
            query = query.Where(p => p.StoreId == storeId);
        }

        query = query.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        return query.ToList();
    }

    public async Task<Product?> GetProductByName(string name, string storeId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(storeId))
            throw new ArgumentException("Store ID cannot be empty", nameof(storeId));

        return _products.FirstOrDefault(p => p.Name == name && p.StoreId == storeId);
    }

    public async Task<bool> UpdateStock(string productName, string storeId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));
        if (string.IsNullOrWhiteSpace(storeId))
            throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
        if (quantity == 0)
            throw new ArgumentException("Quantity cannot be zero", nameof(quantity));

        var product = _products.FirstOrDefault(p => p.Name == productName && p.StoreId == storeId);
        if (product != null)
        {
            product.StockQuantity -= quantity;
            product.LastUpdated = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public async Task<List<Product>> GetAllProducts(string storeId)
    {
        if (string.IsNullOrWhiteSpace(storeId))
            throw new ArgumentException("Store ID cannot be empty", nameof(storeId));

        return _products.Where(p => p.StoreId == storeId).ToList();
    }

    public async Task<bool> ValidateProductExists(string productName, string storeId)
    {
        var product = await GetProductByName(productName, storeId);
        return product != null;
    }

    public async Task<bool> ValidateStockAvailability(string productName, string storeId, int quantity)
    {
        var product = await GetProductByName(productName, storeId);
        if (product == null) return false;
        return product.StockQuantity >= quantity;
    }
}

public class InMemoryStoreService : IStoreService
{
    private readonly List<Store> _stores = new();

    public async Task<List<Store>> GetAllStores()
    {
        return _stores.ToList();
    }

    public async Task<Store?> GetStoreById(string storeId)
    {
        return _stores.FirstOrDefault(s => s.Id == storeId);
    }

    public async Task<string> CreateStore(Store store)
    {
        if (string.IsNullOrEmpty(store.Id))
        {
            store.Id = Guid.NewGuid().ToString();
        }
        _stores.Add(store);
        return store.Id;
    }

    public async Task<bool> UpdateStore(Store store)
    {
        var existing = _stores.FirstOrDefault(s => s.Id == store.Id);
        if (existing != null)
        {
            existing.Name = store.Name;
            existing.Location = store.Location;
            existing.Address = store.Address;
            existing.IsHeadquarters = store.IsHeadquarters;
            existing.Status = store.Status;
            existing.LastSyncTime = store.LastSyncTime;
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteStore(string storeId)
    {
        var store = _stores.FirstOrDefault(s => s.Id == storeId);
        if (store != null)
        {
            _stores.Remove(store);
            return true;
        }
        return false;
    }

    public async Task<bool> SyncStoreData(string storeId)
    {
        var store = await GetStoreById(storeId);
        if (store != null)
        {
            store.LastSyncTime = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public async Task<List<Store>> GetStoresNeedingSync()
    {
        var threshold = DateTime.UtcNow.AddHours(-1);
        return _stores.Where(s => s.LastSyncTime < threshold).ToList();
    }

    public async Task<Dictionary<string, object>> GetStoreStatistics(string storeId)
    {
        var store = await GetStoreById(storeId);
        if (store == null)
            return new Dictionary<string, object>();

        return new Dictionary<string, object>
        {
            ["StoreId"] = store.Id,
            ["StoreName"] = store.Name,
            ["Location"] = store.Location,
            ["Status"] = store.Status,
            ["LastSyncTime"] = store.LastSyncTime
        };
    }
}

public class InMemorySaleService : ISaleService
{
    private readonly List<Sale> _sales = new();
    private readonly IProductService _productService;

    public InMemorySaleService(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<string> CreateSale(Sale sale)
    {
        if (sale == null)
            throw new ArgumentNullException(nameof(sale));

        if (!sale.Items.Any())
            throw new ArgumentException("Sale must have at least one item", nameof(sale));

        if (!await ValidateSaleItems(sale.Items, sale.StoreId))
            throw new InvalidOperationException("One or more items in the sale are invalid");

        // Update stock for each item
        foreach (var item in sale.Items)
        {
            await _productService.UpdateStock(item.ProductName, sale.StoreId, -item.Quantity);
        }

        sale.TotalAmount = await CalculateSaleTotal(sale.Items, sale.StoreId);
        sale.Id = Guid.NewGuid().ToString();
        sale.Date = DateTime.UtcNow;
        _sales.Add(sale);
        return sale.Id;
    }

    public async Task<List<Sale>> GetRecentSales(string storeId, int limit = 10)
    {
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than zero", nameof(limit));
        if (string.IsNullOrWhiteSpace(storeId))
            throw new ArgumentException("Store ID cannot be empty", nameof(storeId));

        return _sales.Where(s => s.StoreId == storeId)
                    .OrderByDescending(s => s.Date)
                    .Take(limit)
                    .ToList();
    }

    public async Task<Sale?> GetSaleById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Sale ID cannot be empty", nameof(id));

        return _sales.FirstOrDefault(s => s.Id == id);
    }

    public async Task<bool> CancelSale(string saleId, string storeId)
    {
        var sale = await GetSaleById(saleId);
        if (sale == null) return false;

        // Restore stock for each item
        foreach (var item in sale.Items)
        {
            await _productService.UpdateStock(item.ProductName, storeId, item.Quantity);
        }

        sale.Status = "Cancelled";
        return true;
    }

    public async Task<decimal> CalculateSaleTotal(List<SaleItem> items, string storeId)
    {
        if (items == null || !items.Any())
            throw new ArgumentException("Items list cannot be empty", nameof(items));
        if (string.IsNullOrWhiteSpace(storeId))
            throw new ArgumentException("Store ID cannot be empty", nameof(storeId));

        decimal total = 0;
        foreach (var item in items)
        {
            var product = await _productService.GetProductByName(item.ProductName, storeId) ?? throw new InvalidOperationException($"Product {item.ProductName} not found");
            total += product.Price * item.Quantity;
        }
        return total;
    }

    public async Task<bool> ValidateSaleItems(List<SaleItem> items, string storeId)
    {
        if (items == null || !items.Any())
            return false;
        if (string.IsNullOrWhiteSpace(storeId))
            throw new ArgumentException("Store ID cannot be empty", nameof(storeId));

        foreach (var item in items)
        {
            if (!await _productService.ValidateProductExists(item.ProductName, storeId))
                return false;

            if (!await _productService.ValidateStockAvailability(item.ProductName, storeId, item.Quantity))
                return false;
        }

        return true;
    }

    public async Task<bool> UpdateSale(Sale sale)
    {
        var existing = _sales.FirstOrDefault(s => s.Id == sale.Id);
        if (existing != null)
        {
            existing.Status = sale.Status;
            existing.TotalAmount = sale.TotalAmount;
            existing.Items = sale.Items;
            return true;
        }
        return false;
    }
}

public class InMemoryDatabaseService : IDatabaseService
{
    private readonly List<Product> _products = new();
    private readonly List<Sale> _sales = new();
    private readonly List<Store> _stores = new();

    public async Task InitializeDatabase()
    {
        // Initialize with some test data
        var testStore = new Store
        {
            Id = "test-store-1",
            Name = "Test Store",
            Location = "Test Location",
            Address = "123 Test Street",
            Status = "Active",
            LastSyncTime = DateTime.UtcNow
        };
        _stores.Add(testStore);

        var testProduct = new Product
        {
            Id = "test-product-1",
            Name = "Test Product",
            Category = "Test Category",
            Price = 10.99m,
            StockQuantity = 100,
            StoreId = testStore.Id,
            LastUpdated = DateTime.UtcNow
        };
        _products.Add(testProduct);
    }

    public async Task<List<Product>> SearchProducts(string searchTerm, string? storeId = null)
    {
        var query = _products.AsQueryable();

        if (!string.IsNullOrEmpty(storeId))
        {
            query = query.Where(p => p.StoreId == storeId);
        }

        query = query.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        return query.ToList();
    }

    public async Task<Product?> GetProductByName(string name, string storeId)
    {
        return _products.FirstOrDefault(p => p.Name == name && p.StoreId == storeId);
    }

    public async Task<Product?> GetProductById(string id)
    {
        return _products.FirstOrDefault(p => p.Id == id);
    }

    public async Task<bool> UpdateProductStock(string productName, string storeId, int quantity)
    {
        var product = _products.FirstOrDefault(p => p.Name == productName && p.StoreId == storeId);
        if (product != null)
        {
            product.StockQuantity -= quantity;
            product.LastUpdated = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public async Task<string> CreateSale(Sale sale)
    {
        sale.Id = Guid.NewGuid().ToString();
        sale.Date = DateTime.UtcNow;
        _sales.Add(sale);
        return sale.Id;
    }

    public async Task<List<Sale>> GetRecentSales(string storeId, int limit = 10)
    {
        return _sales.Where(s => s.StoreId == storeId)
                    .OrderByDescending(s => s.Date)
                    .Take(limit)
                    .ToList();
    }

    public async Task<Sale?> GetSaleById(string saleId)
    {
        return _sales.FirstOrDefault(s => s.Id == saleId);
    }

    public async Task<bool> CancelSale(string saleId)
    {
        var sale = _sales.FirstOrDefault(s => s.Id == saleId);
        if (sale != null)
        {
            sale.Status = "Cancelled";
            return true;
        }
        return false;
    }

    public async Task<List<Product>> GetAllProducts(string? storeId = null)
    {
        if (!string.IsNullOrEmpty(storeId))
        {
            return _products.Where(p => p.StoreId == storeId).ToList();
        }
        return _products.ToList();
    }

    public async Task CreateProduct(Product product)
    {
        product.Id = Guid.NewGuid().ToString();
        product.LastUpdated = DateTime.UtcNow;
        _products.Add(product);
    }

    public async Task<Dictionary<string, object>> GetConsolidatedReport(DateTime startDate, DateTime endDate)
    {
        var salesInRange = _sales.Where(s => s.Date >= startDate && s.Date <= endDate).ToList();

        return new Dictionary<string, object>
        {
            ["TotalStores"] = _stores.Count,
            ["TotalSales"] = salesInRange.Count,
            ["TotalRevenue"] = salesInRange.Sum(s => s.TotalAmount),
            ["StartDate"] = startDate,
            ["EndDate"] = endDate
        };
    }

    public async Task<List<Sale>> GetAllSales()
    {
        return _sales.ToList();
    }

    public async Task DeleteProduct(string id, string storeId)
    {
        var product = _products.FirstOrDefault(p => p.Id == id && p.StoreId == storeId);
        if (product != null)
        {
            _products.Remove(product);
        }
    }

    public async Task UpdateProduct(Product product)
    {
        var existing = _products.FirstOrDefault(p => p.Id == product.Id);
        if (existing != null)
        {
            existing.Name = product.Name;
            existing.Category = product.Category;
            existing.Price = product.Price;
            existing.StockQuantity = product.StockQuantity;
            existing.LastUpdated = DateTime.UtcNow;
        }
    }

    public async Task<bool> UpdateSale(Sale sale)
    {
        var existing = _sales.FirstOrDefault(s => s.Id == sale.Id);
        if (existing != null)
        {
            existing.Status = sale.Status;
            existing.TotalAmount = sale.TotalAmount;
            existing.Items = sale.Items;
            return true;
        }
        return false;
    }
}
