using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CornerShop.Shared.Models;

public class Customer
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [BsonElement("address")]
    public Address? Address { get; set; }

    [BsonElement("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    [BsonElement("registrationDate")]
    public DateTime RegistrationDate { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("preferredStoreId")]
    public string? PreferredStoreId { get; set; }

    [BsonElement("totalOrders")]
    public int TotalOrders { get; set; } = 0;

    [BsonElement("totalSpent")]
    public decimal TotalSpent { get; set; } = 0;

    public Customer()
    {
        RegistrationDate = DateTime.UtcNow;
    }
}

public class Address
{
    [Required]
    [BsonElement("street")]
    public string Street { get; set; } = string.Empty;

    [Required]
    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [Required]
    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    [Required]
    [BsonElement("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = "Canada";
} 