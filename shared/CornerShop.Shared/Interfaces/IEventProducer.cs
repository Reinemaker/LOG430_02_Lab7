using CornerShop.Shared.Models;

namespace CornerShop.Shared.Interfaces
{
    /// <summary>
    /// Interface for publishing business events to message queues
    /// </summary>
    public interface IEventProducer
    {
        /// <summary>
        /// Publish a business event to the appropriate topic
        /// </summary>
        Task PublishEventAsync<T>(T businessEvent, string? correlationId = null) where T : BusinessEvent;

        /// <summary>
        /// Publish an order-related event
        /// </summary>
        Task PublishOrderEventAsync(BusinessEvent orderEvent, string? correlationId = null);

        /// <summary>
        /// Publish an inventory-related event
        /// </summary>
        Task PublishInventoryEventAsync(BusinessEvent inventoryEvent, string? correlationId = null);

        /// <summary>
        /// Publish a payment-related event
        /// </summary>
        Task PublishPaymentEventAsync(BusinessEvent paymentEvent, string? correlationId = null);

        /// <summary>
        /// Publish a saga-related event
        /// </summary>
        Task PublishSagaEventAsync(BusinessEvent sagaEvent, string? correlationId = null);

        /// <summary>
        /// Get the current connection status
        /// </summary>
        Task<bool> IsConnectedAsync();

        /// <summary>
        /// Get event statistics
        /// </summary>
        Task<EventStatistics> GetEventStatisticsAsync();
    }

    /// <summary>
    /// Event statistics for monitoring
    /// </summary>
    public class EventStatistics
    {
        public int TotalEventsPublished { get; set; }
        public int OrderEventsPublished { get; set; }
        public int InventoryEventsPublished { get; set; }
        public int PaymentEventsPublished { get; set; }
        public int SagaEventsPublished { get; set; }
        public DateTime LastEventPublished { get; set; }
        public Dictionary<string, int> EventsByType { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Topic configuration for event organization
    /// </summary>
    public static class EventTopics
    {
        // Main topic categories
        public const string Orders = "orders.events";
        public const string Inventory = "inventory.events";
        public const string Payments = "payments.events";
        public const string Saga = "saga.events";
        public const string Business = "business.events";

        // Specific order event topics
        public const string OrderCreation = "orders.creation";
        public const string OrderConfirmation = "orders.confirmation";
        public const string OrderCancellation = "orders.cancellation";

        // Specific inventory event topics
        public const string StockVerification = "inventory.verification";
        public const string StockReservation = "inventory.reservation";
        public const string StockRelease = "inventory.release";

        // Specific payment event topics
        public const string PaymentProcessing = "payments.processing";
        public const string PaymentCompletion = "payments.completion";
        public const string PaymentFailure = "payments.failure";

        // Specific saga event topics
        public const string SagaOrchestration = "saga.orchestration";
        public const string SagaCompensation = "saga.compensation";
    }
} 