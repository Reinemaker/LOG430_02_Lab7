using CornerShop.Models;

namespace CornerShop.Services
{
    /// <summary>
    /// Interface for publishing saga events from microservices
    /// </summary>
    public interface ISagaEventPublisher
    {
        Task PublishSagaEventAsync(string sagaId, string serviceName, string action, SagaEventType eventType, string? message = null, object? data = null);
        List<SagaStateTransition> GetEventsForSaga(string sagaId);
        List<SagaStateTransition> GetAllEvents();
    }

    /// <summary>
    /// Implementation of saga event publisher using in-memory storage and logging
    /// In a real implementation, this would use a message queue like RabbitMQ or Azure Service Bus
    /// </summary>
    public class SagaEventPublisher : ISagaEventPublisher
    {
        private readonly ILogger<SagaEventPublisher> _logger;
        private readonly Dictionary<string, List<SagaStateTransition>> _eventStore = new();

        public SagaEventPublisher(ILogger<SagaEventPublisher> logger)
        {
            _logger = logger;
        }

        public async Task PublishSagaEventAsync(string sagaId, string serviceName, string action, SagaEventType eventType, string? message = null, object? data = null)
        {
            var transition = new SagaStateTransition
            {
                SagaId = sagaId,
                ServiceName = serviceName,
                Action = action,
                EventType = eventType,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            // Store event in memory (in production, this would be persisted to a database)
            if (!_eventStore.ContainsKey(sagaId))
            {
                _eventStore[sagaId] = new List<SagaStateTransition>();
            }
            _eventStore[sagaId].Add(transition);

            // Log the event
            var logLevel = eventType == SagaEventType.Success ? LogLevel.Information : LogLevel.Error;
            _logger.Log(logLevel, "Saga Event: {SagaId} | {ServiceName} | {Action} | {EventType} | {Message}",
                sagaId, serviceName, action, eventType, message ?? "No message");

            // In a real implementation, this would publish to a message queue
            // await _messageQueue.PublishAsync("saga-events", transition);

            await Task.CompletedTask;
        }

        public List<SagaStateTransition> GetEventsForSaga(string sagaId)
        {
            return _eventStore.ContainsKey(sagaId) ? _eventStore[sagaId] : new List<SagaStateTransition>();
        }

        public List<SagaStateTransition> GetAllEvents()
        {
            return _eventStore.Values.SelectMany(x => x).OrderBy(x => x.Timestamp).ToList();
        }
    }
}
