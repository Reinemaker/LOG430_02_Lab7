using CornerShop.Shared.Models;

namespace CornerShop.Shared.Interfaces;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(string id);
    Task<IEnumerable<Product>> GetProductsByStoreAsync(string storeId);
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category);
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(string id, Product product);
    Task<bool> DeleteProductAsync(string id);
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
    Task<bool> UpdateStockAsync(string productId, int quantity);
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10);
} 