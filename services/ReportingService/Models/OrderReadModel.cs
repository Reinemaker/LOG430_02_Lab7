using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ReportingService.Models;

public class OrderReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [BsonElement("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("items")]
    public List<OrderItemReadModel> Items { get; set; } = new();

    [BsonElement("paymentId")]
    public string? PaymentId { get; set; }

    [BsonElement("paymentStatus")]
    public string? PaymentStatus { get; set; }

    [BsonElement("trackingNumber")]
    public string? TrackingNumber { get; set; }

    [BsonElement("carrier")]
    public string? Carrier { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [BsonElement("confirmedAt")]
    public DateTime? ConfirmedAt { get; set; }

    [BsonElement("shippedAt")]
    public DateTime? ShippedAt { get; set; }

    [BsonElement("deliveredAt")]
    public DateTime? DeliveredAt { get; set; }
}

public class OrderItemReadModel
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    [BsonElement("totalPrice")]
    public decimal TotalPrice { get; set; }
}
