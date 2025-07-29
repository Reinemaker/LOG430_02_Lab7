using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private readonly IMongoCollection<Order> _orders;
    private readonly IDistributedCache _cache;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IMongoDatabase database, IDistributedCache cache, ILogger<OrderService> logger)
    {
        _orders = database.GetCollection<Order>("orders");
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        var cacheKey = "orders:all";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<IEnumerable<Order>>(cached) ?? new List<Order>();
        }

        var orders = await _orders.Find(_ => true).ToListAsync();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orders), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return orders;
    }

    public async Task<Order?> GetOrderByIdAsync(string id)
    {
        var cacheKey = $"order:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<Order>(cached);
        }

        var order = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();

        if (order != null)
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(order), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        }

        return order;
    }

    public async Task<Order?> GetOrderByOrderNumberAsync(string orderNumber)
    {
        var cacheKey = $"order:number:{orderNumber}";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<Order>(cached);
        }

        var order = await _orders.Find(o => o.OrderNumber == orderNumber).FirstOrDefaultAsync();

        if (order != null)
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(order), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        }

        return order;
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerAsync(string customerId)
    {
        var cacheKey = $"orders:customer:{customerId}";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<IEnumerable<Order>>(cached) ?? new List<Order>();
        }

        var orders = await _orders.Find(o => o.CustomerId == customerId).ToListAsync();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orders), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return orders;
    }

    public async Task<IEnumerable<Order>> GetOrdersByStoreAsync(string storeId)
    {
        var cacheKey = $"orders:store:{storeId}";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<IEnumerable<Order>>(cached) ?? new List<Order>();
        }

        var orders = await _orders.Find(o => o.StoreId == storeId).ToListAsync();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orders), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return orders;
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        var cacheKey = $"orders:status:{status}";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<IEnumerable<Order>>(cached) ?? new List<Order>();
        }

        var orders = await _orders.Find(o => o.Status == status).ToListAsync();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orders), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return orders;
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        order.Id = Guid.NewGuid().ToString();
        order.OrderDate = DateTime.UtcNow;

        await _orders.InsertOneAsync(order);

        // Invalidate related caches
        await InvalidateOrderCaches();

        _logger.LogInformation("Order created: {OrderId}", order.Id);
        return order;
    }

    public async Task<Order> UpdateOrderStatusAsync(string id, OrderStatus status)
    {
        var update = Builders<Order>.Update.Set(o => o.Status, status);
        var result = await _orders.UpdateOneAsync(o => o.Id == id, update);

        if (result.ModifiedCount > 0)
        {
            await InvalidateOrderCaches();
            await _cache.RemoveAsync($"order:{id}");

            _logger.LogInformation("Order status updated: {OrderId} -> {Status}", id, status);
        }

        return await GetOrderByIdAsync(id) ?? throw new InvalidOperationException($"Order {id} not found");
    }

    public async Task<Order> UpdatePaymentStatusAsync(string id, PaymentStatus status)
    {
        var update = Builders<Order>.Update.Set(o => o.PaymentStatus, status);
        var result = await _orders.UpdateOneAsync(o => o.Id == id, update);

        if (result.ModifiedCount > 0)
        {
            await InvalidateOrderCaches();
            await _cache.RemoveAsync($"order:{id}");

            _logger.LogInformation("Payment status updated: {OrderId} -> {Status}", id, status);
        }

        return await GetOrderByIdAsync(id) ?? throw new InvalidOperationException($"Order {id} not found");
    }

    public async Task<bool> CancelOrderAsync(string id)
    {
        var update = Builders<Order>.Update.Set(o => o.Status, OrderStatus.Cancelled);
        var result = await _orders.UpdateOneAsync(o => o.Id == id, update);

        if (result.ModifiedCount > 0)
        {
            await InvalidateOrderCaches();
            await _cache.RemoveAsync($"order:{id}");

            _logger.LogInformation("Order cancelled: {OrderId}", id);
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteOrderAsync(string id)
    {
        var result = await _orders.DeleteOneAsync(o => o.Id == id);

        if (result.DeletedCount > 0)
        {
            await InvalidateOrderCaches();
            await _cache.RemoveAsync($"order:{id}");

            _logger.LogInformation("Order deleted: {OrderId}", id);
            return true;
        }

        return false;
    }

    public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Gte(o => o.OrderDate, startDate),
            Builders<Order>.Filter.Lte(o => o.OrderDate, endDate)
        );

        return await _orders.Find(filter).ToListAsync();
    }

    public async Task<Order> ProcessCheckoutAsync(string customerId, string storeId, PaymentMethod paymentMethod, Address? shippingAddress)
    {
        // This would typically integrate with the cart service to get cart items
        // For now, we'll create a basic order structure

        var order = new Order
        {
            CustomerId = customerId,
            StoreId = storeId,
            PaymentMethod = paymentMethod,
            ShippingAddress = shippingAddress,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            OrderType = OrderType.Online,
            Subtotal = 0, // Would be calculated from cart
            TaxAmount = 0, // Would be calculated
            ShippingAmount = 0, // Would be calculated
            TotalAmount = 0, // Would be calculated
            Items = new List<OrderItem>() // Would be populated from cart
        };

        return await CreateOrderAsync(order);
    }

    public async Task<bool> ValidateOrderAsync(Order order)
    {
        // Basic validation logic
        if (string.IsNullOrEmpty(order.CustomerId))
        {
            return false;
        }

        if (string.IsNullOrEmpty(order.StoreId))
        {
            return false;
        }

        if (order.Items == null || !order.Items.Any())
        {
            return false;
        }

        if (order.TotalAmount <= 0)
        {
            return false;
        }

        return true;
    }

    private async Task InvalidateOrderCaches()
    {
        await _cache.RemoveAsync("orders:all");
        // Note: In a production environment, you might want to implement a more sophisticated cache invalidation strategy
    }
}
