using System.Text.Json.Serialization;

namespace CornerShop.Shared.Events;

#region Order Processing Saga Events

// Initiation Events
public class OrderCreatedEvent : BaseEvent
{
    public OrderCreatedEvent(string orderId, string customerId, decimal totalAmount, List<OrderItem> items)
    {
        EventType = "OrderCreated";
        AggregateId = orderId;
        AggregateType = "Order";
        Data = new OrderCreatedData
        {
            OrderId = orderId,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            Items = items,
            CreatedAt = DateTime.UtcNow
        };
        Metadata.SagaId = Guid.NewGuid().ToString();
        Metadata.Step = 1;
        Metadata.TotalSteps = 5;
    }

    public override object Data { get; }
}

public class OrderCreatedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

// Success Events
public class StockReservedEvent : BaseEvent
{
    public StockReservedEvent(string orderId, List<OrderItem> items, string sagaId)
    {
        EventType = "StockReserved";
        AggregateId = orderId;
        AggregateType = "Stock";
        Data = new StockReservedData
        {
            OrderId = orderId,
            Items = items,
            ReservedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Step = 2;
        Metadata.TotalSteps = 5;
    }

    public override object Data { get; }
}

public class StockReservedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = new();

    [JsonPropertyName("reservedAt")]
    public DateTime ReservedAt { get; set; }
}

public class PaymentProcessedEvent : BaseEvent
{
    public PaymentProcessedEvent(string orderId, string customerId, decimal amount, string paymentMethod, string sagaId)
    {
        EventType = "PaymentProcessed";
        AggregateId = orderId;
        AggregateType = "Payment";
        Data = new PaymentProcessedData
        {
            OrderId = orderId,
            CustomerId = customerId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            ProcessedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Step = 3;
        Metadata.TotalSteps = 5;
    }

    public override object Data { get; }
}

public class PaymentProcessedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [JsonPropertyName("processedAt")]
    public DateTime ProcessedAt { get; set; }
}

public class OrderConfirmedEvent : BaseEvent
{
    public OrderConfirmedEvent(string orderId, string customerId, string sagaId)
    {
        EventType = "OrderConfirmed";
        AggregateId = orderId;
        AggregateType = "Order";
        Data = new OrderConfirmedData
        {
            OrderId = orderId,
            CustomerId = customerId,
            ConfirmedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Step = 4;
        Metadata.TotalSteps = 5;
    }

    public override object Data { get; }
}

public class OrderConfirmedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("confirmedAt")]
    public DateTime ConfirmedAt { get; set; }
}

public class NotificationSentEvent : BaseEvent
{
    public NotificationSentEvent(string orderId, string customerId, string notificationType, string sagaId)
    {
        EventType = "NotificationSent";
        AggregateId = orderId;
        AggregateType = "Notification";
        Data = new NotificationSentData
        {
            OrderId = orderId,
            CustomerId = customerId,
            NotificationType = notificationType,
            SentAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Step = 5;
        Metadata.TotalSteps = 5;
    }

    public override object Data { get; }
}

public class NotificationSentData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("notificationType")]
    public string NotificationType { get; set; } = string.Empty;

    [JsonPropertyName("sentAt")]
    public DateTime SentAt { get; set; }
}

// Compensation Events
public class OrderCancelledEvent : BaseEvent
{
    public OrderCancelledEvent(string orderId, string customerId, string reason, string sagaId)
    {
        EventType = "OrderCancelled";
        AggregateId = orderId;
        AggregateType = "Order";
        Data = new OrderCancelledData
        {
            OrderId = orderId,
            CustomerId = customerId,
            Reason = reason,
            CancelledAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Compensation = true;
    }

    public override object Data { get; }
}

public class OrderCancelledData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("cancelledAt")]
    public DateTime CancelledAt { get; set; }
}

public class StockReleasedEvent : BaseEvent
{
    public StockReleasedEvent(string orderId, List<OrderItem> items, string sagaId)
    {
        EventType = "StockReleased";
        AggregateId = orderId;
        AggregateType = "Stock";
        Data = new StockReleasedData
        {
            OrderId = orderId,
            Items = items,
            ReleasedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Compensation = true;
    }

    public override object Data { get; }
}

public class StockReleasedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = new();

    [JsonPropertyName("releasedAt")]
    public DateTime ReleasedAt { get; set; }
}

public class PaymentRefundedEvent : BaseEvent
{
    public PaymentRefundedEvent(string orderId, string customerId, decimal amount, string reason, string sagaId)
    {
        EventType = "PaymentRefunded";
        AggregateId = orderId;
        AggregateType = "Payment";
        Data = new PaymentRefundedData
        {
            OrderId = orderId,
            CustomerId = customerId,
            Amount = amount,
            Reason = reason,
            RefundedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Compensation = true;
    }

    public override object Data { get; }
}

public class PaymentRefundedData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("refundedAt")]
    public DateTime RefundedAt { get; set; }
}

#endregion

#region Saga State Events

public class SagaStartedEvent : BaseEvent
{
    public SagaStartedEvent(string sagaId, string businessProcess, string initiatorId)
    {
        EventType = "SagaStarted";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new SagaStartedData
        {
            SagaId = sagaId,
            BusinessProcess = businessProcess,
            InitiatorId = initiatorId,
            StartedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
    }

    public override object Data { get; }
}

public class SagaStartedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("businessProcess")]
    public string BusinessProcess { get; set; } = string.Empty;

    [JsonPropertyName("initiatorId")]
    public string InitiatorId { get; set; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }
}

public class SagaCompletedEvent : BaseEvent
{
    public SagaCompletedEvent(string sagaId, string businessProcess, string finalEntityId)
    {
        EventType = "SagaCompleted";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new SagaCompletedData
        {
            SagaId = sagaId,
            BusinessProcess = businessProcess,
            FinalEntityId = finalEntityId,
            CompletedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
    }

    public override object Data { get; }
}

public class SagaCompletedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("businessProcess")]
    public string BusinessProcess { get; set; } = string.Empty;

    [JsonPropertyName("finalEntityId")]
    public string FinalEntityId { get; set; } = string.Empty;

    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; set; }
}

public class SagaFailedEvent : BaseEvent
{
    public SagaFailedEvent(string sagaId, string businessProcess, string failureReason, string failedStep)
    {
        EventType = "SagaFailed";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new SagaFailedData
        {
            SagaId = sagaId,
            BusinessProcess = businessProcess,
            FailureReason = failureReason,
            FailedStep = failedStep,
            FailedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
    }

    public override object Data { get; }
}

public class SagaFailedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("businessProcess")]
    public string BusinessProcess { get; set; } = string.Empty;

    [JsonPropertyName("failureReason")]
    public string FailureReason { get; set; } = string.Empty;

    [JsonPropertyName("failedStep")]
    public string FailedStep { get; set; } = string.Empty;

    [JsonPropertyName("failedAt")]
    public DateTime FailedAt { get; set; }
}

public class SagaCompensationStartedEvent : BaseEvent
{
    public SagaCompensationStartedEvent(string sagaId, string businessProcess, string failedStep, string reason)
    {
        EventType = "SagaCompensationStarted";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new SagaCompensationStartedData
        {
            SagaId = sagaId,
            BusinessProcess = businessProcess,
            FailedStep = failedStep,
            Reason = reason,
            CompensationStartedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Compensation = true;
    }

    public override object Data { get; }
}

public class SagaCompensationStartedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("businessProcess")]
    public string BusinessProcess { get; set; } = string.Empty;

    [JsonPropertyName("failedStep")]
    public string FailedStep { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("compensationStartedAt")]
    public DateTime CompensationStartedAt { get; set; }
}

public class SagaCompensationCompletedEvent : BaseEvent
{
    public SagaCompensationCompletedEvent(string sagaId, string businessProcess, List<string> compensatedSteps)
    {
        EventType = "SagaCompensationCompleted";
        AggregateId = sagaId;
        AggregateType = "Saga";
        Data = new SagaCompensationCompletedData
        {
            SagaId = sagaId,
            BusinessProcess = businessProcess,
            CompensatedSteps = compensatedSteps,
            CompensationCompletedAt = DateTime.UtcNow
        };
        Metadata.SagaId = sagaId;
        Metadata.Compensation = true;
    }

    public override object Data { get; }
}

public class SagaCompensationCompletedData
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("businessProcess")]
    public string BusinessProcess { get; set; } = string.Empty;

    [JsonPropertyName("compensatedSteps")]
    public List<string> CompensatedSteps { get; set; } = new();

    [JsonPropertyName("compensationCompletedAt")]
    public DateTime CompensationCompletedAt { get; set; }
}

#endregion
