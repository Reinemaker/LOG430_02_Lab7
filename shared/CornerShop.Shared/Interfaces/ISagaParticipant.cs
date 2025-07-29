using CornerShop.Shared.Models;

namespace CornerShop.Shared.Interfaces
{
    /// <summary>
    /// Interface for saga participants (microservices)
    /// </summary>
    public interface ISagaParticipant
    {
        /// <summary>
        /// Execute a saga step
        /// </summary>
        Task<SagaParticipantResponse> ExecuteStepAsync(SagaParticipantRequest request);

        /// <summary>
        /// Compensate a saga step
        /// </summary>
        Task<SagaCompensationResponse> CompensateStepAsync(SagaCompensationRequest request);

        /// <summary>
        /// Get the service name
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Get supported saga steps
        /// </summary>
        List<string> SupportedSteps { get; }
    }

    /// <summary>
    /// Interface for saga orchestrator
    /// </summary>
    public interface ISagaOrchestrator
    {
        /// <summary>
        /// Execute a saga orchestration
        /// </summary>
        Task<SagaOrchestrationResponse> ExecuteSagaAsync(SagaOrchestrationRequest request);

        /// <summary>
        /// Get saga status
        /// </summary>
        Task<SagaOrchestrationResponse> GetSagaStatusAsync(string sagaId);

        /// <summary>
        /// Compensate a saga
        /// </summary>
        Task<SagaOrchestrationResponse> CompensateSagaAsync(string sagaId, string reason);

        /// <summary>
        /// Get saga metrics
        /// </summary>
        Task<SagaMetrics> GetSagaMetricsAsync();
    }

    /// <summary>
    /// Interface for saga state manager
    /// </summary>
    public interface ISagaStateManager
    {
        /// <summary>
        /// Create a new saga
        /// </summary>
        Task<string> CreateSagaAsync(string sagaType, string orderId, string? correlationId = null);

        /// <summary>
        /// Update saga state
        /// </summary>
        Task UpdateSagaStateAsync(string sagaId, SagaState newState, string serviceName, string action, SagaEventType eventType, string? message = null, object? data = null);

        /// <summary>
        /// Get saga state
        /// </summary>
        Task<SagaState> GetSagaStateAsync(string sagaId);

        /// <summary>
        /// Get saga transitions
        /// </summary>
        Task<List<SagaStateTransition>> GetSagaTransitionsAsync(string sagaId);

        /// <summary>
        /// Complete saga
        /// </summary>
        Task CompleteSagaAsync(string sagaId, string result);

        /// <summary>
        /// Get all active sagas
        /// </summary>
        Task<List<string>> GetActiveSagasAsync();
    }
}
