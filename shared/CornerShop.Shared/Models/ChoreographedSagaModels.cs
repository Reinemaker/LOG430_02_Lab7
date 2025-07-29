using System.Text.Json.Serialization;

namespace CornerShop.Shared.Models;

public enum ChoreographedSagaStatus
{
    InProgress,
    Completed,
    Failed
}

public enum ChoreographedSagaStepStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public class ChoreographedSagaState
{
    [JsonPropertyName("sagaId")]
    public string SagaId { get; set; } = string.Empty;

    [JsonPropertyName("businessProcess")]
    public string BusinessProcess { get; set; } = string.Empty;

    [JsonPropertyName("initiatorId")]
    public string InitiatorId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public ChoreographedSagaStatus Status { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("failedAt")]
    public DateTime? FailedAt { get; set; }

    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; set; }

    [JsonPropertyName("steps")]
    public List<ChoreographedSagaStep> Steps { get; set; } = new();

    [JsonPropertyName("durationSeconds")]
    public double? DurationSeconds => CompletedAt?.Subtract(StartedAt).TotalSeconds;
}

public class ChoreographedSagaStep
{
    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public ChoreographedSagaStepStatus Status { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("failedAt")]
    public DateTime? FailedAt { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("compensated")]
    public bool Compensated { get; set; }

    [JsonPropertyName("compensatedAt")]
    public DateTime? CompensatedAt { get; set; }

    [JsonPropertyName("stepData")]
    public object? StepData { get; set; }

    [JsonPropertyName("durationSeconds")]
    public double? DurationSeconds => CompletedAt?.Subtract(StartedAt ?? DateTime.UtcNow).TotalSeconds;
}

public class ChoreographedSagaStatistics
{
    [JsonPropertyName("totalSagas")]
    public int TotalSagas { get; set; }

    [JsonPropertyName("completedSagas")]
    public int CompletedSagas { get; set; }

    [JsonPropertyName("failedSagas")]
    public int FailedSagas { get; set; }

    [JsonPropertyName("inProgressSagas")]
    public int InProgressSagas { get; set; }

    [JsonPropertyName("compensatedSagas")]
    public int CompensatedSagas { get; set; }

    [JsonPropertyName("averageDurationSeconds")]
    public double AverageDurationSeconds { get; set; }

    [JsonPropertyName("successRate")]
    public double SuccessRate => TotalSagas > 0 ? (double)CompletedSagas / TotalSagas * 100 : 0;

    [JsonPropertyName("failureRate")]
    public double FailureRate => TotalSagas > 0 ? (double)FailedSagas / TotalSagas * 100 : 0;

    [JsonPropertyName("compensationRate")]
    public double CompensationRate => TotalSagas > 0 ? (double)CompensatedSagas / TotalSagas * 100 : 0;

    [JsonPropertyName("businessProcessBreakdown")]
    public List<BusinessProcessStats> BusinessProcessBreakdown { get; set; } = new();
}

public class BusinessProcessStats
{
    [JsonPropertyName("businessProcess")]
    public string BusinessProcess { get; set; } = string.Empty;

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("completedCount")]
    public int CompletedCount { get; set; }

    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    [JsonPropertyName("successRate")]
    public double SuccessRate => TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;

    [JsonPropertyName("failureRate")]
    public double FailureRate => TotalCount > 0 ? (double)FailedCount / TotalCount * 100 : 0;
} 