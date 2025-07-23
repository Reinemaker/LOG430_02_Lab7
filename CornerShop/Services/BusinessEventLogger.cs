using System.Text.Json;

namespace CornerShop.Services
{
    /// <summary>
    /// Implementation of structured business event logging
    /// </summary>
    public class BusinessEventLogger : IBusinessEventLogger
    {
        private readonly ILogger<BusinessEventLogger> _logger;
        private readonly ISagaMetricsService _metricsService;

        public BusinessEventLogger(ILogger<BusinessEventLogger> logger, ISagaMetricsService metricsService)
        {
            _logger = logger;
            _metricsService = metricsService;
        }

        public void LogBusinessEvent(string eventType, string sagaId, string serviceName, Dictionary<string, object> data, string? message = null)
        {
            try
            {
                var structuredLog = GetStructuredLogFormat(eventType, sagaId, serviceName, data, message);
                structuredLog["category"] = "business_event";
                structuredLog["severity"] = "info";

                var jsonLog = JsonSerializer.Serialize(structuredLog, new JsonSerializerOptions { WriteIndented = false });
                _logger.LogInformation("Business Event: {StructuredLog}", jsonLog);

                // Record metrics
                _metricsService.RecordBusinessEvent(eventType, serviceName, sagaId, data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? ""));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log business event {EventType} for saga {SagaId}", eventType, sagaId);
            }
        }

        public void LogBusinessDecision(string decisionType, string sagaId, string serviceName, Dictionary<string, object> data, string? reason = null)
        {
            try
            {
                var structuredLog = GetStructuredLogFormat(decisionType, sagaId, serviceName, data, reason);
                structuredLog["category"] = "business_decision";
                structuredLog["severity"] = "info";
                structuredLog["decision_reason"] = reason ?? "not_provided";

                var jsonLog = JsonSerializer.Serialize(structuredLog, new JsonSerializerOptions { WriteIndented = false });
                _logger.LogInformation("Business Decision: {StructuredLog}", jsonLog);

                // Record metrics
                _metricsService.RecordBusinessEvent($"decision_{decisionType}", serviceName, sagaId, data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? ""));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log business decision {DecisionType} for saga {SagaId}", decisionType, sagaId);
            }
        }

        public void LogSagaLifecycle(string lifecycleEvent, string sagaId, string sagaType, Dictionary<string, object> data, string? message = null)
        {
            try
            {
                var structuredLog = GetStructuredLogFormat(lifecycleEvent, sagaId, "SagaOrchestrator", data, message);
                structuredLog["category"] = "saga_lifecycle";
                structuredLog["severity"] = "info";
                structuredLog["saga_type"] = sagaType;

                var jsonLog = JsonSerializer.Serialize(structuredLog, new JsonSerializerOptions { WriteIndented = false });
                _logger.LogInformation("Saga Lifecycle: {StructuredLog}", jsonLog);

                // Record metrics
                _metricsService.RecordBusinessEvent($"lifecycle_{lifecycleEvent}", "SagaOrchestrator", sagaId, data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? ""));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log saga lifecycle {LifecycleEvent} for saga {SagaId}", lifecycleEvent, sagaId);
            }
        }

        public void LogStateTransition(string sagaId, string sagaType, string fromState, string toState, string serviceName, Dictionary<string, object>? context = null)
        {
            try
            {
                var data = context ?? new Dictionary<string, object>();
                data["from_state"] = fromState;
                data["to_state"] = toState;
                data["saga_type"] = sagaType;

                var structuredLog = GetStructuredLogFormat("state_transition", sagaId, serviceName, data);
                structuredLog["category"] = "state_transition";
                structuredLog["severity"] = "info";
                structuredLog["transition"] = $"{fromState}->{toState}";

                var jsonLog = JsonSerializer.Serialize(structuredLog, new JsonSerializerOptions { WriteIndented = false });
                _logger.LogInformation("State Transition: {StructuredLog}", jsonLog);

                // Record metrics
                _metricsService.RecordStateTransition(sagaType, fromState, toState, serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log state transition {FromState}->{ToState} for saga {SagaId}", fromState, toState, sagaId);
            }
        }

        public void LogCompensation(string sagaId, string sagaType, string stepName, string serviceName, bool isSuccess, Dictionary<string, object>? data = null)
        {
            try
            {
                var compensationData = data ?? new Dictionary<string, object>();
                compensationData["step_name"] = stepName;
                compensationData["saga_type"] = sagaType;
                compensationData["compensation_success"] = isSuccess;

                var structuredLog = GetStructuredLogFormat("compensation", sagaId, serviceName, compensationData);
                structuredLog["category"] = "compensation";
                structuredLog["severity"] = isSuccess ? "info" : "warning";
                structuredLog["compensation_result"] = isSuccess ? "success" : "failure";

                var jsonLog = JsonSerializer.Serialize(structuredLog, new JsonSerializerOptions { WriteIndented = false });
                _logger.LogInformation("Compensation: {StructuredLog}", jsonLog);

                // Record metrics
                _metricsService.RecordBusinessEvent("compensation", serviceName, sagaId, compensationData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? ""));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log compensation for saga {SagaId} step {StepName}", sagaId, stepName);
            }
        }

        public void LogControlledFailure(string sagaId, string failureType, string serviceName, Dictionary<string, object>? data = null)
        {
            try
            {
                var failureData = data ?? new Dictionary<string, object>();
                failureData["failure_type"] = failureType;
                failureData["controlled"] = true;

                var structuredLog = GetStructuredLogFormat("controlled_failure", sagaId, serviceName, failureData);
                structuredLog["category"] = "controlled_failure";
                structuredLog["severity"] = "warning";
                structuredLog["failure_category"] = "simulated";

                var jsonLog = JsonSerializer.Serialize(structuredLog, new JsonSerializerOptions { WriteIndented = false });
                _logger.LogWarning("Controlled Failure: {StructuredLog}", jsonLog);

                // Record metrics
                _metricsService.RecordControlledFailure(failureType, serviceName, sagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log controlled failure {FailureType} for saga {SagaId}", failureType, sagaId);
            }
        }

        public Dictionary<string, object> GetStructuredLogFormat(string eventType, string sagaId, string serviceName, Dictionary<string, object> data, string? message = null)
        {
            return new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["event_type"] = eventType,
                ["saga_id"] = sagaId,
                ["service_name"] = serviceName,
                ["correlation_id"] = sagaId,
                ["message"] = message ?? $"Business event {eventType} occurred",
                ["data"] = data,
                ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                ["version"] = "1.0.0"
            };
        }
    }
}
