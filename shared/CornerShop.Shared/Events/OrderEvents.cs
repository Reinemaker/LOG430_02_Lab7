using System.Text.Json.Serialization;

namespace CornerShop.Shared.Events;

public class OrderCreatedEvent : BaseEvent
{
    public OrderCreatedEvent(string orderId, string cartId, string customerId, decimal totalAmount, List<OrderItem> items)
    {
        EventType = "OrderCreated";
        AggregateId = orderId;
        AggregateType = "Order";
        Data = new OrderCreatedData
        {
            OrderId = orderId,
            CartId = cartId,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            Items = items,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };
    }

    public override object Data { get; }
}

public class OrderCreatedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("cartId")]
    public string CartId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class OrderValidatedEvent : BaseEvent
{
    public OrderValidatedEvent(string orderId, bool isValid, string? validationMessage = null)
    {
        EventType = "OrderValidated";
        AggregateId = orderId;
        AggregateType = "Order";
        Data = new OrderValidatedData
        {
            OrderId = orderId,
            IsValid = isValid,
            ValidationMessage = validationMessage,
            ValidatedAt = DateTime.UtcNow
        };
    }

    public override object Data { get; }
}

public class OrderValidatedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("validationMessage")]
    public string? ValidationMessage { get; set; }

    [JsonPropertyName("validatedAt")]
    public DateTime ValidatedAt { get; set; }
}

public class OrderConfirmedEvent : BaseEvent
{
    public OrderConfirmedEvent(string orderId, string paymentId)
    {
        EventType = "OrderConfirmed";
        AggregateId = orderId;
        AggregateType = "Order";
        Data = new OrderConfirmedData
        {
            OrderId = orderId,
            PaymentId = paymentId,
            Status = "Confirmed",
            ConfirmedAt = DateTime.UtcNow
        };
    }

    public override object Data { get; }
}

public class OrderConfirmedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("paymentId")]
    public string PaymentId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("confirmedAt")]
    public DateTime ConfirmedAt { get; set; }
}

public class OrderShippedEvent : BaseEvent
{
    public OrderShippedEvent(string orderId, string trackingNumber, string carrier)
    {
        EventType = "OrderShipped";
        AggregateId = orderId;
        AggregateType = "Order";
        Data = new OrderShippedData
        {
            OrderId = orderId,
            TrackingNumber = trackingNumber,
            Carrier = carrier,
            Status = "Shipped",
            ShippedAt = DateTime.UtcNow
        };
    }

    public override object Data { get; }
}

public class OrderShippedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("trackingNumber")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("carrier")]
    public string Carrier { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("shippedAt")]
    public DateTime ShippedAt { get; set; }
}

public class OrderDeliveredEvent : BaseEvent
{
    public OrderDeliveredEvent(string orderId)
    {
        EventType = "OrderDelivered";
        AggregateId = orderId;
        AggregateType = "Order";
        Data = new OrderDeliveredData
        {
            OrderId = orderId,
            Status = "Delivered",
            DeliveredAt = DateTime.UtcNow
        };
    }

    public override object Data { get; }
}

public class OrderDeliveredData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("deliveredAt")]
    public DateTime DeliveredAt { get; set; }
}

public class OrderItem
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; set; }
}
