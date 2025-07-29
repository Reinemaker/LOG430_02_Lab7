using CornerShop.Shared.Events;
using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace ChoreographedSagaCoordinator.Services;

public interface IChoreographedSagaCoordinator
{
    Task HandleOrderCreatedEventAsync(OrderCreatedEvent orderCreatedEvent);
    Task HandleStockReservedEventAsync(StockReservedEvent stockReservedEvent);
    Task HandlePaymentProcessedEventAsync(PaymentProcessedEvent paymentProcessedEvent);
    Task HandleOrderConfirmedEventAsync(OrderConfirmedEvent orderConfirmedEvent);
    Task HandleNotificationSentEventAsync(NotificationSentEvent notificationSentEvent);
    Task HandleOrderCancelledEventAsync(OrderCancelledEvent orderCancelledEvent);
    Task HandleStockReleasedEventAsync(StockReleasedEvent stockReleasedEvent);
    Task HandlePaymentRefundedEventAsync(PaymentRefundedEvent paymentRefundedEvent);
    Task<ChoreographedSagaState?> GetSagaStateAsync(string sagaId);
    Task<List<ChoreographedSagaState>> GetAllSagaStatesAsync();
}

public class ChoreographedSagaCoordinatorService : IChoreographedSagaCoordinator
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IEventProducer _eventProducer;
    private readonly ILogger<ChoreographedSagaCoordinatorService> _logger;
    private readonly Counter _sagaStartedCounter;
    private readonly Counter _sagaCompletedCounter;
    private readonly Counter _sagaFailedCounter;
    private readonly Counter _compensationTriggeredCounter;
    private readonly Histogram _sagaDurationHistogram;

    public ChoreographedSagaCoordinatorService(
        IConnectionMultiplexer redis,
        IEventProducer eventProducer,
        ILogger<ChoreographedSagaCoordinatorService> logger)
    {
        _redis = redis;
        _eventProducer = eventProducer;
        _logger = logger;

        // Initialize Prometheus metrics
        _sagaStartedCounter = Metrics.CreateCounter("choreographed_saga_started_total", "Total number of choreographed sagas started");
        _sagaCompletedCounter = Metrics.CreateCounter("choreographed_saga_completed_total", "Total number of choreographed sagas completed");
        _sagaFailedCounter = Metrics.CreateCounter("choreographed_saga_failed_total", "Total number of choreographed sagas failed");
        _compensationTriggeredCounter = Metrics.CreateCounter("choreographed_saga_compensation_triggered_total", "Total number of compensations triggered");
        _sagaDurationHistogram = Metrics.CreateHistogram("choreographed_saga_duration_seconds", "Saga duration in seconds");
    }

    public async Task HandleOrderCreatedEventAsync(OrderCreatedEvent orderCreatedEvent)
    {
        var sagaId = orderCreatedEvent.Metadata.SagaId;
        var orderId = orderCreatedEvent.AggregateId;

        _logger.LogInformation("Handling OrderCreated event for Saga: {SagaId}, Order: {OrderId}", sagaId, orderId);

        // Create saga state
        var sagaState = new ChoreographedSagaState
        {
            SagaId = sagaId,
            BusinessProcess = "OrderProcessing",
            InitiatorId = orderId,
            Status = ChoreographedSagaStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Steps = new List<ChoreographedSagaStep>
            {
                new ChoreographedSagaStep { StepName = "OrderCreated", Status = ChoreographedSagaStepStatus.Completed, CompletedAt = DateTime.UtcNow },
                new ChoreographedSagaStep { StepName = "StockReserved", Status = ChoreographedSagaStepStatus.Pending },
                new ChoreographedSagaStep { StepName = "PaymentProcessed", Status = ChoreographedSagaStepStatus.Pending },
                new ChoreographedSagaStep { StepName = "OrderConfirmed", Status = ChoreographedSagaStepStatus.Pending },
                new ChoreographedSagaStep { StepName = "NotificationSent", Status = ChoreographedSagaStepStatus.Pending }
            }
        };

        await SaveSagaStateAsync(sagaState);

        // Publish saga started event
        var sagaStartedEvent = new SagaStartedEvent(sagaId, "OrderProcessing", orderId);
        await _eventProducer.PublishAsync(sagaStartedEvent, "sagas.events");

        _sagaStartedCounter.Inc();
        _logger.LogInformation("Saga started: {SagaId}", sagaId);
    }

    public async Task HandleStockReservedEventAsync(StockReservedEvent stockReservedEvent)
    {
        var sagaId = stockReservedEvent.Metadata.SagaId;
        var orderId = stockReservedEvent.AggregateId;

        _logger.LogInformation("Handling StockReserved event for Saga: {SagaId}, Order: {OrderId}", sagaId, orderId);

        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null)
        {
            _logger.LogWarning("Saga state not found for Saga: {SagaId}", sagaId);
            return;
        }

        // Update step status
        var stockStep = sagaState.Steps.FirstOrDefault(s => s.StepName == "StockReserved");
        if (stockStep != null)
        {
            stockStep.Status = ChoreographedSagaStepStatus.Completed;
            stockStep.CompletedAt = DateTime.UtcNow;
        }

        sagaState.UpdatedAt = DateTime.UtcNow;
        await SaveSagaStateAsync(sagaState);

        _logger.LogInformation("Stock reserved for Saga: {SagaId}", sagaId);
    }

    public async Task HandlePaymentProcessedEventAsync(PaymentProcessedEvent paymentProcessedEvent)
    {
        var sagaId = paymentProcessedEvent.Metadata.SagaId;
        var orderId = paymentProcessedEvent.AggregateId;

        _logger.LogInformation("Handling PaymentProcessed event for Saga: {SagaId}, Order: {OrderId}", sagaId, orderId);

        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null)
        {
            _logger.LogWarning("Saga state not found for Saga: {SagaId}", sagaId);
            return;
        }

        // Update step status
        var paymentStep = sagaState.Steps.FirstOrDefault(s => s.StepName == "PaymentProcessed");
        if (paymentStep != null)
        {
            paymentStep.Status = ChoreographedSagaStepStatus.Completed;
            paymentStep.CompletedAt = DateTime.UtcNow;
        }

        sagaState.UpdatedAt = DateTime.UtcNow;
        await SaveSagaStateAsync(sagaState);

        _logger.LogInformation("Payment processed for Saga: {SagaId}", sagaId);
    }

    public async Task HandleOrderConfirmedEventAsync(OrderConfirmedEvent orderConfirmedEvent)
    {
        var sagaId = orderConfirmedEvent.Metadata.SagaId;
        var orderId = orderConfirmedEvent.AggregateId;

        _logger.LogInformation("Handling OrderConfirmed event for Saga: {SagaId}, Order: {OrderId}", sagaId, orderId);

        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null)
        {
            _logger.LogWarning("Saga state not found for Saga: {SagaId}", sagaId);
            return;
        }

        // Update step status
        var orderStep = sagaState.Steps.FirstOrDefault(s => s.StepName == "OrderConfirmed");
        if (orderStep != null)
        {
            orderStep.Status = ChoreographedSagaStepStatus.Completed;
            orderStep.CompletedAt = DateTime.UtcNow;
        }

        sagaState.UpdatedAt = DateTime.UtcNow;
        await SaveSagaStateAsync(sagaState);

        _logger.LogInformation("Order confirmed for Saga: {SagaId}", sagaId);
    }

    public async Task HandleNotificationSentEventAsync(NotificationSentEvent notificationSentEvent)
    {
        var sagaId = notificationSentEvent.Metadata.SagaId;
        var orderId = notificationSentEvent.AggregateId;

        _logger.LogInformation("Handling NotificationSent event for Saga: {SagaId}, Order: {OrderId}", sagaId, orderId);

        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null)
        {
            _logger.LogWarning("Saga state not found for Saga: {SagaId}", sagaId);
            return;
        }

        // Update step status
        var notificationStep = sagaState.Steps.FirstOrDefault(s => s.StepName == "NotificationSent");
        if (notificationStep != null)
        {
            notificationStep.Status = ChoreographedSagaStepStatus.Completed;
            notificationStep.CompletedAt = DateTime.UtcNow;
        }

        // Check if all steps are completed
        var allStepsCompleted = sagaState.Steps.All(s => s.Status == ChoreographedSagaStepStatus.Completed);
        if (allStepsCompleted)
        {
            sagaState.Status = ChoreographedSagaStatus.Completed;
            sagaState.CompletedAt = DateTime.UtcNow;
            
            var duration = (sagaState.CompletedAt.Value - sagaState.StartedAt).TotalSeconds;
            _sagaDurationHistogram.Observe(duration);

            // Publish saga completed event
            var sagaCompletedEvent = new SagaCompletedEvent(sagaId, "OrderProcessing", orderId);
            await _eventProducer.PublishAsync(sagaCompletedEvent, "sagas.events");

            _sagaCompletedCounter.Inc();
            _logger.LogInformation("Saga completed successfully: {SagaId}, Duration: {Duration}s", sagaId, duration);
        }

        sagaState.UpdatedAt = DateTime.UtcNow;
        await SaveSagaStateAsync(sagaState);
    }

    public async Task HandleOrderCancelledEventAsync(OrderCancelledEvent orderCancelledEvent)
    {
        var sagaId = orderCancelledEvent.Metadata.SagaId;
        var orderId = orderCancelledEvent.AggregateId;

        _logger.LogInformation("Handling OrderCancelled event for Saga: {SagaId}, Order: {OrderId}", sagaId, orderId);

        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null)
        {
            _logger.LogWarning("Saga state not found for Saga: {SagaId}", sagaId);
            return;
        }

        sagaState.Status = ChoreographedSagaStatus.Failed;
        sagaState.FailedAt = DateTime.UtcNow;
        sagaState.FailureReason = orderCancelledEvent.Data.Reason;
        sagaState.UpdatedAt = DateTime.UtcNow;

        // Publish saga failed event
        var sagaFailedEvent = new SagaFailedEvent(sagaId, "OrderProcessing", orderCancelledEvent.Data.Reason, "OrderCancelled");
        await _eventProducer.PublishAsync(sagaFailedEvent, "sagas.events");

        // Publish compensation started event
        var compensationStartedEvent = new SagaCompensationStartedEvent(sagaId, "OrderProcessing", "OrderCancelled", orderCancelledEvent.Data.Reason);
        await _eventProducer.PublishAsync(compensationStartedEvent, "sagas.events");

        await SaveSagaStateAsync(sagaState);

        _sagaFailedCounter.Inc();
        _compensationTriggeredCounter.Inc();
        _logger.LogWarning("Saga failed and compensation started: {SagaId}, Reason: {Reason}", sagaId, orderCancelledEvent.Data.Reason);
    }

    public async Task HandleStockReleasedEventAsync(StockReleasedEvent stockReleasedEvent)
    {
        var sagaId = stockReleasedEvent.Metadata.SagaId;
        var orderId = stockReleasedEvent.AggregateId;

        _logger.LogInformation("Handling StockReleased event for Saga: {SagaId}, Order: {OrderId}", sagaId, orderId);

        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null)
        {
            _logger.LogWarning("Saga state not found for Saga: {SagaId}", sagaId);
            return;
        }

        // Update compensation status
        var stockStep = sagaState.Steps.FirstOrDefault(s => s.StepName == "StockReserved");
        if (stockStep != null)
        {
            stockStep.Compensated = true;
            stockStep.CompensatedAt = DateTime.UtcNow;
        }

        sagaState.UpdatedAt = DateTime.UtcNow;
        await SaveSagaStateAsync(sagaState);

        _logger.LogInformation("Stock released for Saga: {SagaId}", sagaId);
    }

    public async Task HandlePaymentRefundedEventAsync(PaymentRefundedEvent paymentRefundedEvent)
    {
        var sagaId = paymentRefundedEvent.Metadata.SagaId;
        var orderId = paymentRefundedEvent.AggregateId;

        _logger.LogInformation("Handling PaymentRefunded event for Saga: {SagaId}, Order: {OrderId}", sagaId, orderId);

        var sagaState = await GetSagaStateAsync(sagaId);
        if (sagaState == null)
        {
            _logger.LogWarning("Saga state not found for Saga: {SagaId}", sagaId);
            return;
        }

        // Update compensation status
        var paymentStep = sagaState.Steps.FirstOrDefault(s => s.StepName == "PaymentProcessed");
        if (paymentStep != null)
        {
            paymentStep.Compensated = true;
            paymentStep.CompensatedAt = DateTime.UtcNow;
        }

        // Check if all compensations are completed
        var completedSteps = sagaState.Steps.Where(s => s.Status == ChoreographedSagaStepStatus.Completed).ToList();
        var compensatedSteps = completedSteps.Where(s => s.Compensated).ToList();
        
        if (completedSteps.Count > 0 && completedSteps.Count == compensatedSteps.Count)
        {
            // Publish compensation completed event
            var compensatedStepNames = compensatedSteps.Select(s => s.StepName).ToList();
            var compensationCompletedEvent = new SagaCompensationCompletedEvent(sagaId, "OrderProcessing", compensatedStepNames);
            await _eventProducer.PublishAsync(compensationCompletedEvent, "sagas.events");

            _logger.LogInformation("All compensations completed for Saga: {SagaId}", sagaId);
        }

        sagaState.UpdatedAt = DateTime.UtcNow;
        await SaveSagaStateAsync(sagaState);
    }

    public async Task<ChoreographedSagaState?> GetSagaStateAsync(string sagaId)
    {
        var db = _redis.GetDatabase();
        var sagaStateJson = await db.StringGetAsync($"choreographed_saga:{sagaId}");
        
        if (sagaStateJson.IsNull)
            return null;

        return JsonSerializer.Deserialize<ChoreographedSagaState>(sagaStateJson!);
    }

    public async Task<List<ChoreographedSagaState>> GetAllSagaStatesAsync()
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: "choreographed_saga:*");
        
        var sagaStates = new List<ChoreographedSagaState>();
        
        foreach (var key in keys)
        {
            var sagaStateJson = await db.StringGetAsync(key);
            if (!sagaStateJson.IsNull)
            {
                var sagaState = JsonSerializer.Deserialize<ChoreographedSagaState>(sagaStateJson!);
                if (sagaState != null)
                    sagaStates.Add(sagaState);
            }
        }

        return sagaStates.OrderByDescending(s => s.StartedAt).ToList();
    }

    private async Task SaveSagaStateAsync(ChoreographedSagaState sagaState)
    {
        var db = _redis.GetDatabase();
        var sagaStateJson = JsonSerializer.Serialize(sagaState);
        await db.StringSetAsync($"choreographed_saga:{sagaState.SagaId}", sagaStateJson, TimeSpan.FromHours(24));
    }
} 