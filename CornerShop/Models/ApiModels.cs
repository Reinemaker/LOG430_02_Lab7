using System.Text.Json.Serialization;

namespace CornerShop.Models;

// HATEOAS and API Response Models
public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;

    [JsonPropertyName("links")]
    public List<Link> Links { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Link
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("rel")]
    public string Rel { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
}

public class ErrorResponse
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

// PATCH Request Models
public class ProductPatchRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("stockQuantity")]
    public int? StockQuantity { get; set; }

    [JsonPropertyName("minimumStockLevel")]
    public int? MinimumStockLevel { get; set; }

    [JsonPropertyName("reorderPoint")]
    public int? ReorderPoint { get; set; }
}

public class StorePatchRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("isHeadquarters")]
    public bool? IsHeadquarters { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class SalePatchRequest
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }
}
