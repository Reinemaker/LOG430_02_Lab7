using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CornerShop.Shared.Models;

public class Sale
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [BsonElement("storeId")]
    public string StoreId { get; set; } = string.Empty;

    [BsonElement("customerId")]
    public string? CustomerId { get; set; }

    [Required]
    [BsonElement("date")]
    public DateTime Date { get; set; }

    [Required]
    [BsonElement("items")]
    public List<SaleItem> Items { get; set; } = new List<SaleItem>();

    [Required]
    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Completed"; // Completed, Cancelled, Pending

    [BsonElement("syncStatus")]
    public string SyncStatus { get; set; } = "Pending"; // Pending, Synced, Failed

    [BsonElement("lastSyncAttempt")]
    public DateTime? LastSyncAttempt { get; set; }

    [BsonElement("syncError")]
    public string? SyncError { get; set; }

    [BsonElement("paymentMethod")]
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Credit Card, Online

    [BsonElement("orderType")]
    public string OrderType { get; set; } = "InStore"; // InStore, Online

    public Sale()
    {
        Date = DateTime.UtcNow;
    }
}
