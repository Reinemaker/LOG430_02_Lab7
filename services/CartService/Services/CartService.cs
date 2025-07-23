using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CartService.Services;

public class CartService : ICartService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CartService> _logger;

    public CartService(IDistributedCache cache, ILogger<CartService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<Cart?> GetCartByCustomerIdAsync(string customerId)
    {
        var cacheKey = $"cart:{customerId}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<Cart>(cached);
        }

        return null;
    }

    public async Task<Cart> CreateCartAsync(string customerId)
    {
        var cart = new Cart
        {
            CustomerId = customerId,
            Items = new List<CartItem>(),
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        await SaveCartAsync(cart);
        
        _logger.LogInformation("Cart created for customer: {CustomerId}", customerId);
        return cart;
    }

    public async Task<Cart> AddItemToCartAsync(string customerId, CartItem item)
    {
        var cart = await GetCartByCustomerIdAsync(customerId);
        if (cart == null)
        {
            cart = await CreateCartAsync(customerId);
        }

        // Check if item already exists in cart
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += item.Quantity;
            existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
        }
        else
        {
            cart.Items.Add(item);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await UpdateCartTotalAsync(cart);
        await SaveCartAsync(cart);

        _logger.LogInformation("Item added to cart for customer {CustomerId}: {ProductId}", customerId, item.ProductId);
        return cart;
    }

    public async Task<Cart> UpdateCartItemAsync(string customerId, string productId, int quantity)
    {
        var cart = await GetCartByCustomerIdAsync(customerId);
        if (cart == null)
        {
            throw new InvalidOperationException($"Cart not found for customer {customerId}");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            throw new InvalidOperationException($"Product {productId} not found in cart");
        }

        if (quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
            item.TotalPrice = quantity * item.UnitPrice;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await UpdateCartTotalAsync(cart);
        await SaveCartAsync(cart);

        _logger.LogInformation("Cart item updated for customer {CustomerId}: {ProductId} -> {Quantity}", customerId, productId, quantity);
        return cart;
    }

    public async Task<Cart> RemoveItemFromCartAsync(string customerId, string productId)
    {
        var cart = await GetCartByCustomerIdAsync(customerId);
        if (cart == null)
        {
            throw new InvalidOperationException($"Cart not found for customer {customerId}");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await UpdateCartTotalAsync(cart);
            await SaveCartAsync(cart);

            _logger.LogInformation("Item removed from cart for customer {CustomerId}: {ProductId}", customerId, productId);
        }

        return cart;
    }

    public async Task<bool> ClearCartAsync(string customerId)
    {
        var cart = await GetCartByCustomerIdAsync(customerId);
        if (cart == null)
        {
            return false;
        }

        cart.Items.Clear();
        cart.TotalAmount = 0;
        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(cart);

        _logger.LogInformation("Cart cleared for customer: {CustomerId}", customerId);
        return true;
    }

    public async Task<bool> DeleteCartAsync(string customerId)
    {
        var cacheKey = $"cart:{customerId}";
        await _cache.RemoveAsync(cacheKey);
        
        _logger.LogInformation("Cart deleted for customer: {CustomerId}", customerId);
        return true;
    }

    public async Task<Cart> GetCartByIdAsync(string cartId)
    {
        // This would require a different storage strategy to map cart IDs to customers
        // For now, we'll use customer ID as cart ID
        return await GetCartByCustomerIdAsync(cartId);
    }

    public async Task<bool> UpdateCartTotalAsync(string customerId)
    {
        var cart = await GetCartByCustomerIdAsync(customerId);
        if (cart == null)
        {
            return false;
        }

        await UpdateCartTotalAsync(cart);
        await SaveCartAsync(cart);
        return true;
    }

    public async Task<IEnumerable<Cart>> GetExpiredCartsAsync()
    {
        // This would require a different approach since Redis doesn't support complex queries
        // In a production environment, you might want to use a scheduled job to clean up expired carts
        return new List<Cart>();
    }

    public async Task<bool> CleanupExpiredCartsAsync()
    {
        // This would require a different approach since Redis doesn't support complex queries
        // In a production environment, you might want to use a scheduled job to clean up expired carts
        return true;
    }

    private async Task SaveCartAsync(Cart cart)
    {
        var cacheKey = $"cart:{cart.CustomerId}";
        var cartJson = JsonSerializer.Serialize(cart);
        
        await _cache.SetStringAsync(cacheKey, cartJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
        });
    }

    private async Task UpdateCartTotalAsync(Cart cart)
    {
        cart.TotalAmount = cart.Items.Sum(item => item.TotalPrice);
    }
} 