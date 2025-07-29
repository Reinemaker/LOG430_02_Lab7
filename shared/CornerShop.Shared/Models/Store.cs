using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CornerShop.Shared.Models;

public class Store
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [BsonElement("location")]
    public string Location { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("isHeadquarters")]
    public bool IsHeadquarters { get; set; }

    [BsonElement("lastSyncTime")]
    public DateTime LastSyncTime { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance

    [BsonElement("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [BsonElement("email")]
    public string? Email { get; set; }
}
