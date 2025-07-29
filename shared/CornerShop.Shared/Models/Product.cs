using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CornerShop.Shared.Models;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [Required]
    [BsonElement("price")]
    public decimal Price { get; set; }

    [Required]
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

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("imageUrl")]
    public string? ImageUrl { get; set; }
}
