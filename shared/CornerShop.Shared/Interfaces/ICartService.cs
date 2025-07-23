using CornerShop.Shared.Models;

namespace CornerShop.Shared.Interfaces;

public interface ICartService
{
    Task<Cart?> GetCartByCustomerIdAsync(string customerId);
    Task<Cart> CreateCartAsync(string customerId);
    Task<Cart> AddItemToCartAsync(string customerId, CartItem item);
    Task<Cart> UpdateCartItemAsync(string customerId, string productId, int quantity);
    Task<Cart> RemoveItemFromCartAsync(string customerId, string productId);
    Task<bool> ClearCartAsync(string customerId);
    Task<bool> DeleteCartAsync(string customerId);
    Task<Cart> GetCartByIdAsync(string cartId);
    Task<bool> UpdateCartTotalAsync(string customerId);
    Task<IEnumerable<Cart>> GetExpiredCartsAsync();
    Task<bool> CleanupExpiredCartsAsync();
} 