using Confluent.Kafka;
using CornerShop.Shared.Interfaces;
using System.Text.Json;

namespace EventPublisher;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(ILogger<KafkaEventPublisher> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka:29092",
            ClientId = "cornerShop-event-publisher",
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T @event, string topic) where T : Events.BaseEvent
    {
        await PublishAsync(@event, topic, @event.AggregateId);
    }

    public async Task PublishAsync<T>(T @event, string topic, string key) where T : Events.BaseEvent
    {
        try
        {
            var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var message = new Message<string, string>
            {
                Key = key,
                Value = json,
                Headers = new Headers
                {
                    { "eventType", System.Text.Encoding.UTF8.GetBytes(@event.EventType) },
                    { "aggregateType", System.Text.Encoding.UTF8.GetBytes(@event.AggregateType) },
                    { "correlationId", System.Text.Encoding.UTF8.GetBytes(@event.Metadata.CorrelationId) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message);
            
            _logger.LogInformation("Event published successfully: {EventType} to topic {Topic} with key {Key} at partition {Partition} offset {Offset}",
                @event.EventType, topic, key, result.Partition, result.Offset);

            // Increment metrics
            IncrementEventPublishedMetric(@event.EventType, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to topic {Topic}", @event.EventType, topic);
            throw;
        }
    }

    public async Task PublishBatchAsync<T>(IEnumerable<T> events, string topic) where T : Events.BaseEvent
    {
        var tasks = events.Select(@event => PublishAsync(@event, topic));
        await Task.WhenAll(tasks);
        
        _logger.LogInformation("Published {Count} events to topic {Topic}", events.Count(), topic);
    }

    private void IncrementEventPublishedMetric(string eventType, string topic)
    {
        // This would integrate with Prometheus metrics
        // For now, just log the metric
        _logger.LogInformation("METRIC: Event published - Type: {EventType}, Topic: {Topic}", eventType, topic);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
} 