using System.Text.Json.Serialization;

namespace CornerShop.Shared.Events;

public class CartCreatedEvent : BaseEvent
{
    public CartCreatedEvent(string cartId, string customerId)
    {
        EventType = "CartCreated";
        AggregateId = cartId;
        AggregateType = "Cart";
        Data = new CartCreatedData
        {
            CartId = cartId,
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public override object Data { get; }
}

public class CartCreatedData
{
    [JsonPropertyName("cartId")]
    public string CartId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class ItemAddedToCartEvent : BaseEvent
{
    public ItemAddedToCartEvent(string cartId, string productId, int quantity, decimal unitPrice)
    {
        EventType = "ItemAddedToCart";
        AggregateId = cartId;
        AggregateType = "Cart";
        Data = new ItemAddedToCartData
        {
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            AddedAt = DateTime.UtcNow
        };
    }

    public override object Data { get; }
}

public class ItemAddedToCartData
{
    [JsonPropertyName("cartId")]
    public string CartId { get; set; } = string.Empty;

    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("addedAt")]
    public DateTime AddedAt { get; set; }
}

public class CartCheckedOutEvent : BaseEvent
{
    public CartCheckedOutEvent(string cartId, string customerId, decimal totalAmount)
    {
        EventType = "CartCheckedOut";
        AggregateId = cartId;
        AggregateType = "Cart";
        Data = new CartCheckedOutData
        {
            CartId = cartId,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            CheckedOutAt = DateTime.UtcNow
        };
    }

    public override object Data { get; }
}

public class CartCheckedOutData
{
    [JsonPropertyName("cartId")]
    public string CartId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("checkedOutAt")]
    public DateTime CheckedOutAt { get; set; }
}

public class CartExpiredEvent : BaseEvent
{
    public CartExpiredEvent(string cartId)
    {
        EventType = "CartExpired";
        AggregateId = cartId;
        AggregateType = "Cart";
        Data = new CartExpiredData
        {
            CartId = cartId,
            ExpiredAt = DateTime.UtcNow
        };
    }

    public override object Data { get; }
}

public class CartExpiredData
{
    [JsonPropertyName("cartId")]
    public string CartId { get; set; } = string.Empty;

    [JsonPropertyName("expiredAt")]
    public DateTime ExpiredAt { get; set; }
} 