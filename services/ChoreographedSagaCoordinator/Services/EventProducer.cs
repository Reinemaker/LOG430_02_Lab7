using CornerShop.Shared.Events;
using CornerShop.Shared.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace ChoreographedSagaCoordinator.Services;

public class EventProducer : IEventProducer
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<EventProducer> _logger;

    public EventProducer(IConnectionMultiplexer redis, ILogger<EventProducer> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishAsync(BaseEvent @event, string streamName)
    {
        try
        {
            var db = _redis.GetDatabase();
            var eventData = JsonSerializer.Serialize(@event);
            
            var streamEntries = new NameValueEntry[]
            {
                new NameValueEntry("EventType", @event.EventType),
                new NameValueEntry("Data", eventData),
                new NameValueEntry("Timestamp", DateTime.UtcNow.ToString("O"))
            };

            var messageId = await db.StreamAddAsync(streamName, streamEntries);
            
            _logger.LogDebug("Published event {EventType} to stream {StreamName} with ID {MessageId}", 
                @event.EventType, streamName, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType} to stream {StreamName}", 
                @event.EventType, streamName);
            throw;
        }
    }

    public async Task PublishAsync<T>(T eventData, string streamName) where T : class
    {
        try
        {
            var db = _redis.GetDatabase();
            var serializedData = JsonSerializer.Serialize(eventData);
            
            var streamEntries = new NameValueEntry[]
            {
                new NameValueEntry("EventType", typeof(T).Name),
                new NameValueEntry("Data", serializedData),
                new NameValueEntry("Timestamp", DateTime.UtcNow.ToString("O"))
            };

            var messageId = await db.StreamAddAsync(streamName, streamEntries);
            
            _logger.LogDebug("Published event {EventType} to stream {StreamName} with ID {MessageId}", 
                typeof(T).Name, streamName, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType} to stream {StreamName}", 
                typeof(T).Name, streamName);
            throw;
        }
    }
} 