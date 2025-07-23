using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CornerShop.Models;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("storeId")]
    public string StoreId { get; set; } = string.Empty;

    [BsonElement("stock")]
    public int StockQuantity { get; set; }

    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("minimumStockLevel")]
    public int MinimumStockLevel { get; set; }

    [BsonElement("reorderPoint")]
    public int ReorderPoint { get; set; }
}
