using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json;

namespace EventStore.Models;

public class StoredEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("eventId")]
    public string EventId { get; set; } = string.Empty;

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("aggregateId")]
    public string AggregateId { get; set; } = string.Empty;

    [BsonElement("aggregateType")]
    public string AggregateType { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("data")]
    public string Data { get; set; } = string.Empty;

    [BsonElement("metadata")]
    public string Metadata { get; set; } = string.Empty;

    [BsonElement("topic")]
    public string Topic { get; set; } = string.Empty;

    [BsonElement("partition")]
    public int Partition { get; set; }

    [BsonElement("offset")]
    public long Offset { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static StoredEvent FromBaseEvent(CornerShop.Shared.Events.BaseEvent @event, string topic, int partition, long offset)
    {
        return new StoredEvent
        {
            EventId = @event.EventId,
            EventType = @event.EventType,
            AggregateId = @event.AggregateId,
            AggregateType = @event.AggregateType,
            Timestamp = @event.Timestamp,
            Version = @event.Version,
            Data = JsonSerializer.Serialize(@event.Data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            Metadata = JsonSerializer.Serialize(@event.Metadata, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            Topic = topic,
            Partition = partition,
            Offset = offset
        };
    }
} 