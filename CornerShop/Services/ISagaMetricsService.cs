using Prometheus;

namespace CornerShop.Services
{
    /// <summary>
    /// Service for tracking Prometheus metrics for saga orchestration
    /// </summary>
    public interface ISagaMetricsService
    {
        /// <summary>
        /// Record saga start
        /// </summary>
        void RecordSagaStart(string sagaType, string sagaId);

        /// <summary>
        /// Record saga completion
        /// </summary>
        void RecordSagaCompletion(string sagaType, string sagaId, bool isSuccess, TimeSpan duration);

        /// <summary>
        /// Record saga step execution
        /// </summary>
        void RecordSagaStep(string sagaType, string stepName, string serviceName, bool isSuccess, TimeSpan duration);

        /// <summary>
        /// Record saga state transition
        /// </summary>
        void RecordStateTransition(string sagaType, string fromState, string toState, string serviceName);

        /// <summary>
        /// Record compensation action
        /// </summary>
        void RecordCompensation(string sagaType, string stepName, string serviceName, bool isSuccess, TimeSpan duration);

        /// <summary>
        /// Record controlled failure
        /// </summary>
        void RecordControlledFailure(string failureType, string serviceName, string sagaId);

        /// <summary>
        /// Record business event
        /// </summary>
        void RecordBusinessEvent(string eventType, string serviceName, string sagaId, Dictionary<string, string>? labels = null);

        /// <summary>
        /// Get current metrics summary
        /// </summary>
        Dictionary<string, object> GetMetricsSummary();
    }
}
