using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CornerShop.Shared.Models;

public class SaleItem
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