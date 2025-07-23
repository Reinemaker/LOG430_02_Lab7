using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CornerShop.Models;

public class Sale
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("storeId")]
    public string StoreId { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("items")]
    public List<SaleItem> Items { get; set; } = new List<SaleItem>();

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

    public Sale()
    {
        Date = DateTime.UtcNow;
    }
}
