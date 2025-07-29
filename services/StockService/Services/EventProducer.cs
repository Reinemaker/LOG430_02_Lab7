using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace StockService.Services;

public class EventProducer : IEventProducer
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<EventProducer> _logger;
    private readonly Dictionary<string, int> _eventCounters = new();

    public EventProducer(IConnectionMultiplexer redis, ILogger<EventProducer> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishEventAsync<T>(T businessEvent, string? correlationId = null) where T : BusinessEvent
    {
        try
        {
            businessEvent.Source = "StockService";
            if (!string.IsNullOrEmpty(correlationId))
            {
                businessEvent.CorrelationId = correlationId;
            }

            var jsonEvent = JsonSerializer.Serialize(businessEvent);
            var db = _redis.GetDatabase();

            // Add to general business events stream
            await db.StreamAddAsync(EventTopics.BusinessEvents, "event", jsonEvent);

            // Add to specific topic based on event type
            var topic = GetTopicForEventType(businessEvent.EventType);
            await db.StreamAddAsync(topic, "event", jsonEvent);

            // Update statistics
            lock (_eventCounters)
            {
                _eventCounters[businessEvent.EventType] = _eventCounters.GetValueOrDefault(businessEvent.EventType, 0) + 1;
            }

            _logger.LogInformation("Published event {EventType} with ID {EventId} to topic {Topic}", 
                businessEvent.EventType, businessEvent.EventId, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType}", businessEvent.EventType);
            throw;
        }
    }

    public async Task PublishOrderEventAsync(BusinessEvent orderEvent, string? correlationId = null)
    {
        await PublishEventAsync(orderEvent, correlationId);
    }

    public async Task PublishInventoryEventAsync(BusinessEvent inventoryEvent, string? correlationId = null)
    {
        await PublishEventAsync(inventoryEvent, correlationId);
    }

    public async Task PublishPaymentEventAsync(BusinessEvent paymentEvent, string? correlationId = null)
    {
        await PublishEventAsync(paymentEvent, correlationId);
    }

    public async Task PublishSagaEventAsync(BusinessEvent sagaEvent, string? correlationId = null)
    {
        await PublishEventAsync(sagaEvent, correlationId);
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<EventStatistics> GetEventStatisticsAsync()
    {
        var db = _redis.GetDatabase();
        
        var statistics = new EventStatistics
        {
            TotalEvents = 0,
            EventsByType = new Dictionary<string, int>(),
            LastPublished = DateTime.UtcNow,
            IsConnected = await IsConnectedAsync()
        };

        lock (_eventCounters)
        {
            statistics.EventsByType = new Dictionary<string, int>(_eventCounters);
            statistics.TotalEvents = _eventCounters.Values.Sum();
        }

        return statistics;
    }

    private string GetTopicForEventType(string eventType)
    {
        return eventType switch
        {
            "StockVerified" => EventTopics.InventoryEvents,
            "StockReserved" => EventTopics.InventoryEvents,
            "StockReleased" => EventTopics.InventoryEvents,
            "OrderCreated" => EventTopics.OrderEvents,
            "OrderConfirmed" => EventTopics.OrderEvents,
            "OrderCancelled" => EventTopics.OrderEvents,
            "PaymentProcessed" => EventTopics.PaymentEvents,
            "PaymentFailed" => EventTopics.PaymentEvents,
            "SagaStarted" => EventTopics.SagaEvents,
            "SagaCompleted" => EventTopics.SagaEvents,
            "SagaCompensated" => EventTopics.SagaEvents,
            _ => EventTopics.BusinessEvents
        };
    }
} 