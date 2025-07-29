using System.Text.Json.Serialization;

namespace CornerShop.Shared.Events;

public abstract class BaseEvent
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("aggregateId")]
    public string AggregateId { get; set; } = string.Empty;

    [JsonPropertyName("aggregateType")]
    public string AggregateType { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("metadata")]
    public EventMetadata Metadata { get; set; } = new();

    [JsonPropertyName("data")]
    public abstract object Data { get; }
}

public class EventMetadata
{
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("causationId")]
    public string? CausationId { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }

    [JsonPropertyName("sagaId")]
    public string? SagaId { get; set; }

    [JsonPropertyName("step")]
    public int? Step { get; set; }

    [JsonPropertyName("totalSteps")]
    public int? TotalSteps { get; set; }
} 