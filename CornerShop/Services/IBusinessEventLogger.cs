namespace CornerShop.Services
{
    /// <summary>
    /// Service for structured logging of business events and decisions
    /// </summary>
    public interface IBusinessEventLogger
    {
        /// <summary>
        /// Log a business event with structured data
        /// </summary>
        void LogBusinessEvent(string eventType, string sagaId, string serviceName, Dictionary<string, object> data, string? message = null);

        /// <summary>
        /// Log a business decision with structured data
        /// </summary>
        void LogBusinessDecision(string decisionType, string sagaId, string serviceName, Dictionary<string, object> data, string? reason = null);

        /// <summary>
        /// Log a saga lifecycle event
        /// </summary>
        void LogSagaLifecycle(string lifecycleEvent, string sagaId, string sagaType, Dictionary<string, object> data, string? message = null);

        /// <summary>
        /// Log a state transition with context
        /// </summary>
        void LogStateTransition(string sagaId, string sagaType, string fromState, string toState, string serviceName, Dictionary<string, object>? context = null);

        /// <summary>
        /// Log a compensation action
        /// </summary>
        void LogCompensation(string sagaId, string sagaType, string stepName, string serviceName, bool isSuccess, Dictionary<string, object>? data = null);

        /// <summary>
        /// Log a controlled failure event
        /// </summary>
        void LogControlledFailure(string sagaId, string failureType, string serviceName, Dictionary<string, object>? data = null);

        /// <summary>
        /// Get structured log format for external systems
        /// </summary>
        Dictionary<string, object> GetStructuredLogFormat(string eventType, string sagaId, string serviceName, Dictionary<string, object> data, string? message = null);
    }
}
