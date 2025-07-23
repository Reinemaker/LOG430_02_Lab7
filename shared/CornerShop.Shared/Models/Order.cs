using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CornerShop.Shared.Models;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [BsonElement("storeId")]
    public string StoreId { get; set; } = string.Empty;

    [Required]
    [BsonElement("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [BsonElement("items")]
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    [Required]
    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    [BsonElement("taxAmount")]
    public decimal TaxAmount { get; set; }

    [BsonElement("shippingAmount")]
    public decimal ShippingAmount { get; set; }

    [Required]
    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("status")]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [BsonElement("orderType")]
    public OrderType OrderType { get; set; } = OrderType.Online;

    [BsonElement("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;

    [BsonElement("paymentStatus")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [BsonElement("shippingAddress")]
    public Address? ShippingAddress { get; set; }

    [BsonElement("billingAddress")]
    public Address? BillingAddress { get; set; }

    [BsonElement("orderDate")]
    public DateTime OrderDate { get; set; }

    [BsonElement("estimatedDeliveryDate")]
    public DateTime? EstimatedDeliveryDate { get; set; }

    [BsonElement("actualDeliveryDate")]
    public DateTime? ActualDeliveryDate { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    public Order()
    {
        OrderDate = DateTime.UtcNow;
        OrderNumber = GenerateOrderNumber();
    }

    private string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
}

public class OrderItem
{
    [Required]
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [Required]
    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    [Required]
    [BsonElement("totalPrice")]
    public decimal TotalPrice { get; set; }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}

public enum OrderType
{
    InStore,
    Online,
    Phone
}

public enum PaymentMethod
{
    Cash,
    CreditCard,
    DebitCard,
    PayPal,
    BankTransfer
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Failed,
    Refunded
} 