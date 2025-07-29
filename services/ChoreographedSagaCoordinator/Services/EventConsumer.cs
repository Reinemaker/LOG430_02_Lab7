using CornerShop.Shared.Events;
using CornerShop.Shared.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace ChoreographedSagaCoordinator.Services;

public interface IEventConsumer
{
    Task StartConsumingAsync();
    Task StopConsumingAsync();
}

public class EventConsumer : IEventConsumer, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IChoreographedSagaCoordinator _sagaCoordinator;
    private readonly ILogger<EventConsumer> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _consumingTask;
    private bool _disposed = false;

    public EventConsumer(
        IConnectionMultiplexer redis,
        IChoreographedSagaCoordinator sagaCoordinator,
        ILogger<EventConsumer> logger)
    {
        _redis = redis;
        _sagaCoordinator = sagaCoordinator;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _consumingTask = Task.Run(ConsumeEventsAsync);
    }

    public async Task StartConsumingAsync()
    {
        _logger.LogInformation("Starting event consumer for choreographed saga events");
        // The consuming task is already started in the constructor
    }

    public async Task StopConsumingAsync()
    {
        _logger.LogInformation("Stopping event consumer");
        _cancellationTokenSource.Cancel();
        
        try
        {
            await _consumingTask;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Event consumer stopped successfully");
        }
    }

    private async Task ConsumeEventsAsync()
    {
        var db = _redis.GetDatabase();
        var streamName = "sagas.events";
        var consumerGroupName = "choreographed-saga-coordinator";
        var consumerName = $"consumer-{Guid.NewGuid():N}";

        try
        {
            // Create consumer group if it doesn't exist
            try
            {
                await db.StreamCreateConsumerGroupAsync(streamName, consumerGroupName, StreamPosition.NewMessages);
                _logger.LogInformation("Created consumer group: {ConsumerGroup}", consumerGroupName);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                _logger.LogInformation("Consumer group already exists: {ConsumerGroup}", consumerGroupName);
            }

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Read events from the stream
                    var messages = await db.StreamReadGroupAsync(
                        streamName,
                        consumerGroupName,
                        consumerName,
                        ">",
                        count: 10,
                        noAck: false);

                    foreach (var message in messages)
                    {
                        await ProcessMessageAsync(message);
                        await db.StreamAcknowledgeAsync(streamName, consumerGroupName, message.Id);
                    }

                    // Wait a bit before polling again
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming events from stream: {StreamName}", streamName);
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in event consumer");
        }
    }

    private async Task ProcessMessageAsync(StreamEntry message)
    {
        try
        {
            var eventType = message.Values.FirstOrDefault(v => v.Name == "EventType").Value.ToString();
            var eventData = message.Values.FirstOrDefault(v => v.Name == "Data").Value.ToString();

            _logger.LogDebug("Processing event: {EventType}", eventType);

            switch (eventType)
            {
                case "OrderCreated":
                    var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(eventData);
                    if (orderCreatedEvent != null)
                        await _sagaCoordinator.HandleOrderCreatedEventAsync(orderCreatedEvent);
                    break;

                case "StockReserved":
                    var stockReservedEvent = JsonSerializer.Deserialize<StockReservedEvent>(eventData);
                    if (stockReservedEvent != null)
                        await _sagaCoordinator.HandleStockReservedEventAsync(stockReservedEvent);
                    break;

                case "PaymentProcessed":
                    var paymentProcessedEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(eventData);
                    if (paymentProcessedEvent != null)
                        await _sagaCoordinator.HandlePaymentProcessedEventAsync(paymentProcessedEvent);
                    break;

                case "OrderConfirmed":
                    var orderConfirmedEvent = JsonSerializer.Deserialize<OrderConfirmedEvent>(eventData);
                    if (orderConfirmedEvent != null)
                        await _sagaCoordinator.HandleOrderConfirmedEventAsync(orderConfirmedEvent);
                    break;

                case "NotificationSent":
                    var notificationSentEvent = JsonSerializer.Deserialize<NotificationSentEvent>(eventData);
                    if (notificationSentEvent != null)
                        await _sagaCoordinator.HandleNotificationSentEventAsync(notificationSentEvent);
                    break;

                case "OrderCancelled":
                    var orderCancelledEvent = JsonSerializer.Deserialize<OrderCancelledEvent>(eventData);
                    if (orderCancelledEvent != null)
                        await _sagaCoordinator.HandleOrderCancelledEventAsync(orderCancelledEvent);
                    break;

                case "StockReleased":
                    var stockReleasedEvent = JsonSerializer.Deserialize<StockReleasedEvent>(eventData);
                    if (stockReleasedEvent != null)
                        await _sagaCoordinator.HandleStockReleasedEventAsync(stockReleasedEvent);
                    break;

                case "PaymentRefunded":
                    var paymentRefundedEvent = JsonSerializer.Deserialize<PaymentRefundedEvent>(eventData);
                    if (paymentRefundedEvent != null)
                        await _sagaCoordinator.HandlePaymentRefundedEventAsync(paymentRefundedEvent);
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {MessageId}", message.Id);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
} 