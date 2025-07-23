using CornerShop.Models;

namespace CornerShop.Services
{
    /// <summary>
    /// Interface for managing saga state machine with persistence
    /// </summary>
    public interface ISagaStateManager
    {
        Task<SagaStateMachine> CreateSagaAsync(string sagaId, string sagaType);
        Task<SagaStateMachine?> GetSagaAsync(string sagaId);
        Task UpdateSagaStateAsync(string sagaId, SagaState newState, string serviceName, string action, SagaEventType eventType, string? message = null, object? data = null);
        Task<List<SagaStateMachine>> GetAllSagasAsync();
        Task<List<SagaStateMachine>> GetSagasByStateAsync(SagaState state);
        Task<List<SagaStateTransition>> GetSagaTransitionsAsync(string sagaId);
        Task PersistSagaAsync(SagaStateMachine saga);
    }

    /// <summary>
    /// Implementation of saga state manager with in-memory persistence
    /// In production, this would use a database like MongoDB or SQL Server
    /// </summary>
    public class SagaStateManager : ISagaStateManager
    {
        private readonly ILogger<SagaStateManager> _logger;
        private readonly Dictionary<string, SagaStateMachine> _sagaStore = new();
        private readonly ISagaEventPublisher _eventPublisher;

        public SagaStateManager(ILogger<SagaStateManager> logger, ISagaEventPublisher eventPublisher)
        {
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        public async Task<SagaStateMachine> CreateSagaAsync(string sagaId, string sagaType)
        {
            var saga = new SagaStateMachine
            {
                SagaId = sagaId,
                SagaType = sagaType,
                CurrentState = SagaState.Started,
                CreatedAt = DateTime.UtcNow
            };

            _sagaStore[sagaId] = saga;

            // Publish initial event
            await _eventPublisher.PublishSagaEventAsync(sagaId, "SagaOrchestrator", "SagaStarted", SagaEventType.Success, $"Saga {sagaType} started", new { SagaType = sagaType });

            _logger.LogInformation("Created saga {SagaId} of type {SagaType}", sagaId, sagaType);

            return saga;
        }

        public async Task<SagaStateMachine?> GetSagaAsync(string sagaId)
        {
            return _sagaStore.TryGetValue(sagaId, out var saga) ? saga : null;
        }

        public async Task UpdateSagaStateAsync(string sagaId, SagaState newState, string serviceName, string action, SagaEventType eventType, string? message = null, object? data = null)
        {
            if (!_sagaStore.TryGetValue(sagaId, out var saga))
            {
                throw new InvalidOperationException($"Saga {sagaId} not found");
            }

            var previousState = saga.CurrentState;
            saga.TransitionTo(newState, serviceName, action, eventType, message, data);

            // Publish state change event
            await _eventPublisher.PublishSagaEventAsync(sagaId, serviceName, action, eventType, message, data);

            _logger.LogInformation("Saga {SagaId} state changed from {PreviousState} to {NewState} by {ServiceName}",
                sagaId, previousState, newState, serviceName);

            // Persist the updated saga
            await PersistSagaAsync(saga);
        }

        public async Task<List<SagaStateMachine>> GetAllSagasAsync()
        {
            return _sagaStore.Values.OrderByDescending(s => s.CreatedAt).ToList();
        }

        public async Task<List<SagaStateMachine>> GetSagasByStateAsync(SagaState state)
        {
            return _sagaStore.Values.Where(s => s.CurrentState == state).OrderByDescending(s => s.CreatedAt).ToList();
        }

        public async Task<List<SagaStateTransition>> GetSagaTransitionsAsync(string sagaId)
        {
            if (!_sagaStore.TryGetValue(sagaId, out var saga))
            {
                return new List<SagaStateTransition>();
            }

            return saga.Transitions.OrderBy(t => t.Timestamp).ToList();
        }

        public async Task PersistSagaAsync(SagaStateMachine saga)
        {
            // In production, this would save to a database
            // For now, we just update the in-memory store
            _sagaStore[saga.SagaId] = saga;

            _logger.LogDebug("Persisted saga {SagaId} with state {State}", saga.SagaId, saga.CurrentState);
        }
    }
}
