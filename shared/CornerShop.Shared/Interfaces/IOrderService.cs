using CornerShop.Shared.Models;

namespace CornerShop.Shared.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<Order?> GetOrderByIdAsync(string id);
    Task<Order?> GetOrderByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetOrdersByCustomerAsync(string customerId);
    Task<IEnumerable<Order>> GetOrdersByStoreAsync(string storeId);
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status);
    Task<Order> CreateOrderAsync(Order order);
    Task<Order> UpdateOrderStatusAsync(string id, OrderStatus status);
    Task<Order> UpdatePaymentStatusAsync(string id, PaymentStatus status);
    Task<bool> CancelOrderAsync(string id);
    Task<bool> DeleteOrderAsync(string id);
    Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Order> ProcessCheckoutAsync(string customerId, string storeId, PaymentMethod paymentMethod, Address? shippingAddress);
    Task<bool> ValidateOrderAsync(Order order);
} 