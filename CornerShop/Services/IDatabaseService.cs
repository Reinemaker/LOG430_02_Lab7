using System.Collections.Generic;
using System.Threading.Tasks;
using CornerShop.Models;

namespace CornerShop.Services;

public interface IDatabaseService
{
    Task InitializeDatabase();
    Task<List<Product>> SearchProducts(string searchTerm, string? storeId = null);
    Task<Product?> GetProductByName(string name, string storeId);
    Task<Product?> GetProductById(string id);
    Task<bool> UpdateProductStock(string productName, string storeId, int quantity);
    Task<string> CreateSale(Sale sale);
    Task<List<Sale>> GetRecentSales(string storeId, int limit = 10);
    Task<Sale?> GetSaleById(string saleId);
    Task<bool> CancelSale(string saleId);
    Task<List<Product>> GetAllProducts(string? storeId = null);
    Task CreateProduct(Product product);
    Task<Dictionary<string, object>> GetConsolidatedReport(DateTime startDate, DateTime endDate);
    Task<List<Sale>> GetAllSales();
    Task DeleteProduct(string id, string storeId);
    Task UpdateProduct(Product product);
    Task<bool> UpdateSale(Sale sale);
}
