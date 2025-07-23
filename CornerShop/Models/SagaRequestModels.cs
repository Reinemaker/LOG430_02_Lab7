using System.Text.Json.Serialization;

namespace CornerShop.Models
{
    /// <summary>
    /// Request model for creating sales through saga orchestration
    /// </summary>
    public class CreateSaleRequest
    {
        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<CreateSaleItemRequest> Items { get; set; } = new();
    }

    /// <summary>
    /// Request model for sale items in saga orchestration
    /// </summary>
    public class CreateSaleItemRequest
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }
    }

    /// <summary>
    /// Request model for creating orders through saga orchestration
    /// </summary>
    public class CreateOrderRequest
    {
        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<CreateSaleItemRequest> Items { get; set; } = new();

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for stock updates through saga orchestration
    /// </summary>
    public class StockUpdateRequest
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty; // "add", "remove", "reserve"
    }
}
