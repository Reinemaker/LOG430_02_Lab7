using System.Text.Json.Serialization;

namespace CornerShop.Shared.Models
{
    /// <summary>
    /// Base class for all business events with common properties
    /// </summary>
    public abstract class BusinessEvent
    {
        [JsonPropertyName("eventId")]
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Order management events
    /// </summary>
    public class OrderCreatedEvent : BusinessEvent
    {
        public OrderCreatedEvent()
        {
            EventType = "OrderCreated";
        }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("items")]
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderConfirmedEvent : BusinessEvent
    {
        public OrderConfirmedEvent()
        {
            EventType = "OrderConfirmed";
        }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("confirmationTime")]
        public DateTime ConfirmationTime { get; set; }

        [JsonPropertyName("finalAmount")]
        public decimal FinalAmount { get; set; }
    }

    public class OrderCancelledEvent : BusinessEvent
    {
        public OrderCancelledEvent()
        {
            EventType = "OrderCancelled";
        }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("cancellationReason")]
        public string CancellationReason { get; set; } = string.Empty;

        [JsonPropertyName("cancelledBy")]
        public string CancelledBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Inventory management events
    /// </summary>
    public class StockVerifiedEvent : BusinessEvent
    {
        public StockVerifiedEvent()
        {
            EventType = "StockVerified";
        }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("verificationResult")]
        public bool IsAvailable { get; set; }

        [JsonPropertyName("verificationDetails")]
        public List<StockVerificationDetail> VerificationDetails { get; set; } = new List<StockVerificationDetail>();
    }

    public class StockReservedEvent : BusinessEvent
    {
        public StockReservedEvent()
        {
            EventType = "StockReserved";
        }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("reservationId")]
        public string ReservationId { get; set; } = string.Empty;

        [JsonPropertyName("reservedItems")]
        public List<StockReservation> ReservedItems { get; set; } = new List<StockReservation>();
    }

    public class StockReleasedEvent : BusinessEvent
    {
        public StockReleasedEvent()
        {
            EventType = "StockReleased";
        }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("reservationId")]
        public string ReservationId { get; set; } = string.Empty;

        [JsonPropertyName("releaseReason")]
        public string ReleaseReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Payment events
    /// </summary>
    public class PaymentProcessedEvent : BusinessEvent
    {
        public PaymentProcessedEvent()
        {
            EventType = "PaymentProcessed";
        }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // Success, Failed
    }

    public class PaymentFailedEvent : BusinessEvent
    {
        public PaymentFailedEvent()
        {
            EventType = "PaymentFailed";
        }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("failureReason")]
        public string FailureReason { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Saga orchestration events
    /// </summary>
    public class SagaStartedEvent : BusinessEvent
    {
        public SagaStartedEvent()
        {
            EventType = "SagaStarted";
        }

        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("sagaType")]
        public string SagaType { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;
    }

    public class SagaCompletedEvent : BusinessEvent
    {
        public SagaCompletedEvent()
        {
            EventType = "SagaCompleted";
        }

        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("sagaType")]
        public string SagaType { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty; // Success, Failed, Compensated
    }

    public class SagaCompensatedEvent : BusinessEvent
    {
        public SagaCompensatedEvent()
        {
            EventType = "SagaCompensated";
        }

        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("sagaType")]
        public string SagaType { get; set; } = string.Empty;

        [JsonPropertyName("compensationReason")]
        public string CompensationReason { get; set; } = string.Empty;

        [JsonPropertyName("compensatedSteps")]
        public List<string> CompensatedSteps { get; set; } = new List<string>();
    }

    /// <summary>
    /// Supporting data models
    /// </summary>
    public class OrderItem
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }
    }

    public class StockVerificationDetail
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("requestedQuantity")]
        public int RequestedQuantity { get; set; }

        [JsonPropertyName("availableQuantity")]
        public int AvailableQuantity { get; set; }

        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }
    }

    public class StockReservation
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("reservationId")]
        public string ReservationId { get; set; } = Guid.NewGuid().ToString();
    }
} 