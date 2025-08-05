using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace CornerShop.Shared.Models;

public enum SagaStatus
{
    Started,
    InProgress,
    Completed,
    Compensating,
    Failed,
    Aborted
}

public class SagaState
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("status")]
    public SagaStatus Status { get; set; }

    [BsonElement("currentStep")]
    public int CurrentStep { get; set; }

    [BsonElement("totalSteps")]
    public int TotalSteps { get; set; }

    [BsonElement("failedStep")]
    public int? FailedStep { get; set; }

    [BsonElement("failureReason")]
    public string? FailureReason { get; set; }

    [BsonElement("steps")]
    public List<SagaStep> Steps { get; set; } = new();

    [BsonElement("compensationData")]
    public Dictionary<string, object> CompensationData { get; set; } = new();

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("failedAt")]
    public DateTime? FailedAt { get; set; }
}

public class SagaStep
{
    [BsonElement("stepNumber")]
    public int StepNumber { get; set; }

    [BsonElement("stepName")]
    public string StepName { get; set; } = string.Empty;

    [BsonElement("status")]
    public SagaStepStatus Status { get; set; }

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("failedAt")]
    public DateTime? FailedAt { get; set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("stepData")]
    public object? StepData { get; set; }

    [BsonElement("compensationData")]
    public object? CompensationData { get; set; }

    [BsonElement("compensatedAt")]
    public DateTime? CompensatedAt { get; set; }
}

public enum SagaStepStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Compensated
}

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
        { SendNotifications, "SendFailureNotification" }
    };
}
