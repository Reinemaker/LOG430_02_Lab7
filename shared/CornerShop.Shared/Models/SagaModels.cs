using System.Text.Json.Serialization;

namespace CornerShop.Shared.Models
{
    /// <summary>
    /// Saga state enumeration
    /// </summary>
    public enum SagaState
    {
        Started,
        StockVerifying,
        StockVerified,
        StockReserving,
        StockReserved,
        PaymentProcessing,
        PaymentProcessed,
        OrderConfirming,
        Completed,
        Failed,
        Compensating,
        Compensated
    }

    /// <summary>
    /// Saga event type enumeration
    /// </summary>
    public enum SagaEventType
    {
        Success,
        Failure,
        Compensation
    }

    /// <summary>
    /// Saga step information
    /// </summary>
    public class SagaStep
    {
        [JsonPropertyName("stepId")]
        public string StepId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("stepName")]
        public string StepName { get; set; } = string.Empty;

        [JsonPropertyName("serviceName")]
        public string ServiceName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed, Compensated

        [JsonPropertyName("startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("compensationRequired")]
        public bool CompensationRequired { get; set; }

        [JsonPropertyName("compensatedAt")]
        public DateTime? CompensatedAt { get; set; }
    }

    /// <summary>
    /// Saga state transition record
    /// </summary>
    public class SagaStateTransition
    {
        [JsonPropertyName("transitionId")]
        public string TransitionId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("fromState")]
        public SagaState FromState { get; set; }

        [JsonPropertyName("toState")]
        public SagaState ToState { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("serviceName")]
        public string ServiceName { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("eventType")]
        public SagaEventType EventType { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    /// <summary>
    /// Saga orchestration request
    /// </summary>
    public class SagaOrchestrationRequest
    {
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("sagaType")]
        public string SagaType { get; set; } = "OrderCreation";

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty;

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// Saga orchestration response
    /// </summary>
    public class SagaOrchestrationResponse
    {
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // Success, Failed, Compensated

        [JsonPropertyName("currentState")]
        public SagaState CurrentState { get; set; }

        [JsonPropertyName("steps")]
        public List<SagaStep> Steps { get; set; } = new List<SagaStep>();

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("duration")]
        public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt);
    }

    /// <summary>
    /// Saga participant request
    /// </summary>
    public class SagaParticipantRequest
    {
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("stepName")]
        public string StepName { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// Saga participant response
    /// </summary>
    public class SagaParticipantResponse
    {
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("stepName")]
        public string StepName { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("compensationRequired")]
        public bool CompensationRequired { get; set; }
    }

    /// <summary>
    /// Saga compensation request
    /// </summary>
    public class SagaCompensationRequest
    {
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("stepName")]
        public string StepName { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// Saga compensation response
    /// </summary>
    public class SagaCompensationResponse
    {
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("stepName")]
        public string StepName { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Saga metrics
    /// </summary>
    public class SagaMetrics
    {
        [JsonPropertyName("totalSagas")]
        public int TotalSagas { get; set; }

        [JsonPropertyName("successfulSagas")]
        public int SuccessfulSagas { get; set; }

        [JsonPropertyName("failedSagas")]
        public int FailedSagas { get; set; }

        [JsonPropertyName("compensatedSagas")]
        public int CompensatedSagas { get; set; }

        [JsonPropertyName("averageDuration")]
        public TimeSpan AverageDuration { get; set; }

        [JsonPropertyName("sagasByState")]
        public Dictionary<SagaState, int> SagasByState { get; set; } = new Dictionary<SagaState, int>();

        [JsonPropertyName("sagasByType")]
        public Dictionary<string, int> SagasByType { get; set; } = new Dictionary<string, int>();
    }
}
