using CornerShop.Models;

namespace CornerShop.Services
{
    public interface IProductService
    {
        Task<List<Product>> SearchProducts(string searchTerm, string? storeId = null);
        Task<Product?> GetProductByName(string name, string storeId);
        Task<bool> UpdateStock(string productName, string storeId, int quantity, string? sagaId = null);
        Task<List<Product>> GetAllProducts(string storeId);
        Task<bool> ValidateProductExists(string productName, string storeId, string? sagaId = null);
        Task<bool> ValidateStockAvailability(string productName, string storeId, int quantity, string? sagaId = null);
    }
}
