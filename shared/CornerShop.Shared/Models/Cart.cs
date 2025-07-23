using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CornerShop.Shared.Models;

public class Cart
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("items")]
    public List<CartItem> Items { get; set; } = new List<CartItem>();

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    public Cart()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(30); // Cart expires after 30 days
    }
}

public class CartItem
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

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; }

    public CartItem()
    {
        AddedAt = DateTime.UtcNow;
    }
} 