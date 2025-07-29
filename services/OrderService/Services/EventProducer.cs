using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace OrderService.Services
{
    /// <summary>
    /// Event Producer Service - Publishes business events to Redis Streams
    /// </summary>
    public class EventProducer : IEventProducer
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<EventProducer> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly EventStatistics _statistics;

        public EventProducer(IConnectionMultiplexer redis, ILogger<EventProducer> logger)
        {
            _redis = redis;
            _logger = logger;
            _statistics = new EventStatistics();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task PublishEventAsync<T>(T businessEvent, string? correlationId = null) where T : BusinessEvent
        {
            try
            {
                // Set correlation ID if provided
                if (!string.IsNullOrEmpty(correlationId))
                {
                    businessEvent.CorrelationId = correlationId;
                }

                // Set source information
                businessEvent.Source = "OrderService";

                // Determine the appropriate topic based on event type
                string topic = GetTopicForEventType(businessEvent.EventType);

                // Serialize the event to JSON
                string eventJson = JsonSerializer.Serialize(businessEvent, _jsonOptions);

                // Get Redis database
                var db = _redis.GetDatabase();

                // Publish to Redis Stream
                var streamKey = topic;
                var entryId = await db.StreamAddAsync(streamKey, new NameValueEntry[]
                {
                    new NameValueEntry("eventId", businessEvent.EventId),
                    new NameValueEntry("eventType", businessEvent.EventType),
                    new NameValueEntry("timestamp", businessEvent.Timestamp.ToString("O")),
                    new NameValueEntry("correlationId", businessEvent.CorrelationId ?? ""),
                    new NameValueEntry("source", businessEvent.Source),
                    new NameValueEntry("version", businessEvent.Version),
                    new NameValueEntry("data", eventJson)
                });

                // Update statistics
                UpdateStatistics(businessEvent.EventType);

                _logger.LogInformation("Event published successfully: {EventType} | {EventId} | {Topic} | {EntryId}",
                    businessEvent.EventType, businessEvent.EventId, topic, entryId);

                // Also publish to the main events topic for general subscribers
                await PublishToMainTopicAsync(businessEvent, eventJson, db);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event: {EventType} | {EventId}",
                    businessEvent.EventType, businessEvent.EventId);
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
            return await Task.FromResult(_statistics);
        }

        private string GetTopicForEventType(string eventType)
        {
            return eventType switch
            {
                // Order events
                "OrderCreated" => EventTopics.OrderCreation,
                "OrderConfirmed" => EventTopics.OrderConfirmation,
                "OrderCancelled" => EventTopics.OrderCancellation,

                // Inventory events
                "StockVerified" => EventTopics.StockVerification,
                "StockReserved" => EventTopics.StockReservation,
                "StockReleased" => EventTopics.StockRelease,

                // Payment events
                "PaymentProcessed" => EventTopics.PaymentCompletion,
                "PaymentFailed" => EventTopics.PaymentFailure,

                // Saga events
                "SagaStarted" => EventTopics.SagaOrchestration,
                "SagaCompleted" => EventTopics.SagaOrchestration,
                "SagaCompensated" => EventTopics.SagaCompensation,

                // Default to main category topics
                var type when type.StartsWith("Order") => EventTopics.Orders,
                var type when type.StartsWith("Stock") => EventTopics.Inventory,
                var type when type.StartsWith("Payment") => EventTopics.Payments,
                var type when type.StartsWith("Saga") => EventTopics.Saga,
                _ => EventTopics.Business // Default fallback
            };
        }

        private async Task PublishToMainTopicAsync(BusinessEvent businessEvent, string eventJson, IDatabase db)
        {
            try
            {
                var mainTopic = EventTopics.Business;
                await db.StreamAddAsync(mainTopic, new NameValueEntry[]
                {
                    new NameValueEntry("eventId", businessEvent.EventId),
                    new NameValueEntry("eventType", businessEvent.EventType),
                    new NameValueEntry("timestamp", businessEvent.Timestamp.ToString("O")),
                    new NameValueEntry("correlationId", businessEvent.CorrelationId ?? ""),
                    new NameValueEntry("source", businessEvent.Source),
                    new NameValueEntry("version", businessEvent.Version),
                    new NameValueEntry("data", eventJson)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish to main topic for event: {EventType}", businessEvent.EventType);
            }
        }

        private void UpdateStatistics(string eventType)
        {
            lock (_statistics)
            {
                _statistics.TotalEventsPublished++;
                _statistics.LastEventPublished = DateTime.UtcNow;

                // Update category counts
                if (eventType.StartsWith("Order"))
                {
                    _statistics.OrderEventsPublished++;
                }
                else if (eventType.StartsWith("Stock"))
                {
                    _statistics.InventoryEventsPublished++;
                }
                else if (eventType.StartsWith("Payment"))
                {
                    _statistics.PaymentEventsPublished++;
                }
                else if (eventType.StartsWith("Saga"))
                {
                    _statistics.SagaEventsPublished++;
                }

                // Update event type counts
                if (!_statistics.EventsByType.ContainsKey(eventType))
                {
                    _statistics.EventsByType[eventType] = 0;
                }
                _statistics.EventsByType[eventType]++;
            }
        }
    }
}
