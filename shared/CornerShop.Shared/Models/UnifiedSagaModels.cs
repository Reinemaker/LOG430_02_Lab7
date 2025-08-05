using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CornerShop.Shared.Models
{
    /// <summary>
    /// Unified saga status enumeration
    /// </summary>
    public enum SagaStatus
    {
        Started,
        InProgress,
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
        Compensated,
        Aborted
    }

    /// <summary>
    /// Unified saga step status enumeration
    /// </summary>
    public enum SagaStepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Compensated
    }

    /// <summary>
    /// Unified saga event type enumeration
    /// </summary>
    public enum SagaEventType
    {
        Success,
        Failure,
        Compensation
    }

    /// <summary>
    /// Unified saga step information
    /// </summary>
    public class SagaStep
    {
        [JsonPropertyName("stepId")]
        [BsonElement("stepId")]
        public string StepId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("stepNumber")]
        [BsonElement("stepNumber")]
        public int StepNumber { get; set; }

        [JsonPropertyName("stepName")]
        [BsonElement("stepName")]
        public string StepName { get; set; } = string.Empty;

        [JsonPropertyName("serviceName")]
        [BsonElement("serviceName")]
        public string ServiceName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        [BsonElement("status")]
        public SagaStepStatus Status { get; set; }

        [JsonPropertyName("startedAt")]
        [BsonElement("startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("failedAt")]
        [BsonElement("failedAt")]
        public DateTime? FailedAt { get; set; }

        [JsonPropertyName("errorMessage")]
        [BsonElement("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("compensationRequired")]
        [BsonElement("compensationRequired")]
        public bool CompensationRequired { get; set; }

        [JsonPropertyName("compensated")]
        [BsonElement("compensated")]
        public bool Compensated { get; set; }

        [JsonPropertyName("compensatedAt")]
        [BsonElement("compensatedAt")]
        public DateTime? CompensatedAt { get; set; }

        [JsonPropertyName("stepData")]
        [BsonElement("stepData")]
        public object? StepData { get; set; }

        [JsonPropertyName("compensationData")]
        [BsonElement("compensationData")]
        public object? CompensationData { get; set; }

        [JsonPropertyName("durationSeconds")]
        public double? DurationSeconds => CompletedAt?.Subtract(StartedAt ?? DateTime.UtcNow).TotalSeconds;
    }

    /// <summary>
    /// Unified saga state for both orchestrated and choreographed sagas
    /// </summary>
    public class UnifiedSagaState
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("sagaId")]
        [BsonElement("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        [BsonElement("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("customerId")]
        [BsonElement("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonPropertyName("businessProcess")]
        [BsonElement("businessProcess")]
        public string BusinessProcess { get; set; } = string.Empty;

        [JsonPropertyName("initiatorId")]
        [BsonElement("initiatorId")]
        public string InitiatorId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        [BsonElement("status")]
        public SagaStatus Status { get; set; }

        [JsonPropertyName("currentStep")]
        [BsonElement("currentStep")]
        public int CurrentStep { get; set; }

        [JsonPropertyName("totalSteps")]
        [BsonElement("totalSteps")]
        public int TotalSteps { get; set; }

        [JsonPropertyName("failedStep")]
        [BsonElement("failedStep")]
        public int? FailedStep { get; set; }

        [JsonPropertyName("failureReason")]
        [BsonElement("failureReason")]
        public string? FailureReason { get; set; }

        [JsonPropertyName("steps")]
        [BsonElement("steps")]
        public List<SagaStep> Steps { get; set; } = new();

        [JsonPropertyName("compensationData")]
        [BsonElement("compensationData")]
        public Dictionary<string, object> CompensationData { get; set; } = new();

        [JsonPropertyName("startedAt")]
        [BsonElement("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("completedAt")]
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("failedAt")]
        [BsonElement("failedAt")]
        public DateTime? FailedAt { get; set; }

        [JsonPropertyName("durationSeconds")]
        public double? DurationSeconds => CompletedAt?.Subtract(StartedAt).TotalSeconds;
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
        public SagaStatus FromState { get; set; }

        [JsonPropertyName("toState")]
        public SagaStatus ToState { get; set; }

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
    /// Unified saga orchestration request
    /// </summary>
    public class SagaOrchestrationRequest
    {
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("sagaType")]
        public string SagaType { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<object> Items { get; set; } = new();

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty;

        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Unified saga orchestration response
    /// </summary>
    public class SagaOrchestrationResponse
    {
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public SagaStatus Status { get; set; }

        [JsonPropertyName("currentState")]
        public string CurrentState { get; set; } = string.Empty;

        [JsonPropertyName("steps")]
        public List<SagaStep> Steps { get; set; } = new();

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
    /// Unified saga participant request
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
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Unified saga participant response
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
    /// Unified saga compensation request
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
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Unified saga compensation response
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
    /// Unified saga metrics
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
        public Dictionary<SagaStatus, int> SagasByState { get; set; } = new();

        [JsonPropertyName("sagasByType")]
        public Dictionary<string, int> SagasByType { get; set; } = new();
    }

    /// <summary>
    /// Static class containing saga step constants
    /// </summary>
    public static class SagaSteps
    {
        public const string CreateOrder = "CreateOrder";
        public const string ReserveStock = "ReserveStock";
        public const string ProcessPayment = "ProcessPayment";
        public const string ConfirmOrder = "ConfirmOrder";
        public const string SendNotifications = "SendNotifications";

        public static readonly Dictionary<string, int> StepNumbers = new()
        {
            { CreateOrder, 1 },
            { ReserveStock, 2 },
            { ProcessPayment, 3 },
            { ConfirmOrder, 4 },
            { SendNotifications, 5 }
        };

        public static readonly Dictionary<string, string> CompensationSteps = new()
        {
            { CreateOrder, "CancelOrder" },
            { ReserveStock, "ReleaseStock" },
            { ProcessPayment, "RefundPayment" },
            { ConfirmOrder, "CancelOrder" },
            { SendNotifications, "CancelNotifications" }
        };
    }
}
