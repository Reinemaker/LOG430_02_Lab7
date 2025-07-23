using Prometheus;
using CornerShop.Models;

namespace CornerShop.Services
{
    /// <summary>
    /// Implementation of saga metrics service with Prometheus metrics
    /// </summary>
    public class SagaMetricsService : ISagaMetricsService
    {
        private readonly ILogger<SagaMetricsService> _logger;
        private readonly Dictionary<string, DateTime> _sagaStartTimes = new();

        // Prometheus Metrics
        private readonly Counter _sagaTotalCounter;
        private readonly Counter _sagaSuccessCounter;
        private readonly Counter _sagaFailureCounter;
        private readonly Histogram _sagaDurationHistogram;
        private readonly Counter _sagaStepTotalCounter;
        private readonly Counter _sagaStepSuccessCounter;
        private readonly Counter _sagaStepFailureCounter;
        private readonly Histogram _sagaStepDurationHistogram;
        private readonly Counter _stateTransitionCounter;
        private readonly Counter _compensationTotalCounter;
        private readonly Counter _compensationSuccessCounter;
        private readonly Counter _compensationFailureCounter;
        private readonly Histogram _compensationDurationHistogram;
        private readonly Counter _controlledFailureCounter;
        private readonly Counter _businessEventCounter;
        private readonly Gauge _activeSagasGauge;
        private readonly Gauge _sagasByStateGauge;

        public SagaMetricsService(ILogger<SagaMetricsService> logger)
        {
            _logger = logger;

            // Initialize Prometheus metrics
            _sagaTotalCounter = Metrics.CreateCounter("saga_total", "Total number of sagas", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type" }
            });

            _sagaSuccessCounter = Metrics.CreateCounter("saga_success_total", "Total number of successful sagas", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type" }
            });

            _sagaFailureCounter = Metrics.CreateCounter("saga_failure_total", "Total number of failed sagas", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type", "failure_reason" }
            });

            _sagaDurationHistogram = Metrics.CreateHistogram("saga_duration_seconds", "Saga execution duration in seconds", new HistogramConfiguration
            {
                LabelNames = new[] { "saga_type", "status" },
                Buckets = new[] { 0.1, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0 }
            });

            _sagaStepTotalCounter = Metrics.CreateCounter("saga_step_total", "Total number of saga steps", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type", "step_name", "service_name" }
            });

            _sagaStepSuccessCounter = Metrics.CreateCounter("saga_step_success_total", "Total number of successful saga steps", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type", "step_name", "service_name" }
            });

            _sagaStepFailureCounter = Metrics.CreateCounter("saga_step_failure_total", "Total number of failed saga steps", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type", "step_name", "service_name", "error_type" }
            });

            _sagaStepDurationHistogram = Metrics.CreateHistogram("saga_step_duration_seconds", "Saga step execution duration in seconds", new HistogramConfiguration
            {
                LabelNames = new[] { "saga_type", "step_name", "service_name", "status" },
                Buckets = new[] { 0.01, 0.05, 0.1, 0.5, 1.0, 2.0, 5.0 }
            });

            _stateTransitionCounter = Metrics.CreateCounter("saga_state_transition_total", "Total number of state transitions", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type", "from_state", "to_state", "service_name" }
            });

            _compensationTotalCounter = Metrics.CreateCounter("saga_compensation_total", "Total number of compensation actions", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type", "step_name", "service_name" }
            });

            _compensationSuccessCounter = Metrics.CreateCounter("saga_compensation_success_total", "Total number of successful compensation actions", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type", "step_name", "service_name" }
            });

            _compensationFailureCounter = Metrics.CreateCounter("saga_compensation_failure_total", "Total number of failed compensation actions", new CounterConfiguration
            {
                LabelNames = new[] { "saga_type", "step_name", "service_name", "error_type" }
            });

            _compensationDurationHistogram = Metrics.CreateHistogram("saga_compensation_duration_seconds", "Compensation action duration in seconds", new HistogramConfiguration
            {
                LabelNames = new[] { "saga_type", "step_name", "service_name", "status" },
                Buckets = new[] { 0.01, 0.05, 0.1, 0.5, 1.0, 2.0, 5.0 }
            });

            _controlledFailureCounter = Metrics.CreateCounter("controlled_failure_total", "Total number of controlled failures", new CounterConfiguration
            {
                LabelNames = new[] { "failure_type", "service_name" }
            });

            _businessEventCounter = Metrics.CreateCounter("business_event_total", "Total number of business events", new CounterConfiguration
            {
                LabelNames = new[] { "event_type", "service_name" }
            });

            _activeSagasGauge = Metrics.CreateGauge("saga_active", "Number of currently active sagas", new GaugeConfiguration
            {
                LabelNames = new[] { "saga_type" }
            });

            _sagasByStateGauge = Metrics.CreateGauge("saga_by_state", "Number of sagas by state", new GaugeConfiguration
            {
                LabelNames = new[] { "saga_type", "state" }
            });
        }

        public void RecordSagaStart(string sagaType, string sagaId)
        {
            try
            {
                _sagaTotalCounter.WithLabels(sagaType).Inc();
                _activeSagasGauge.WithLabels(sagaType).Inc();
                _sagaStartTimes[sagaId] = DateTime.UtcNow;

                _logger.LogInformation("Saga metrics: Started {SagaType} with ID {SagaId}", sagaType, sagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record saga start metrics for {SagaType} {SagaId}", sagaType, sagaId);
            }
        }

        public void RecordSagaCompletion(string sagaType, string sagaId, bool isSuccess, TimeSpan duration)
        {
            try
            {
                var status = isSuccess ? "success" : "failure";
                var durationSeconds = duration.TotalSeconds;

                if (isSuccess)
                {
                    _sagaSuccessCounter.WithLabels(sagaType).Inc();
                }
                else
                {
                    _sagaFailureCounter.WithLabels(sagaType, "unknown").Inc();
                }

                _sagaDurationHistogram.WithLabels(sagaType, status).Observe(durationSeconds);
                _activeSagasGauge.WithLabels(sagaType).Dec();

                // Remove from start times
                _sagaStartTimes.Remove(sagaId);

                _logger.LogInformation("Saga metrics: Completed {SagaType} {SagaId} with status {Status} in {Duration:F2}s",
                    sagaType, sagaId, status, durationSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record saga completion metrics for {SagaType} {SagaId}", sagaType, sagaId);
            }
        }

        public void RecordSagaStep(string sagaType, string stepName, string serviceName, bool isSuccess, TimeSpan duration)
        {
            try
            {
                var status = isSuccess ? "success" : "failure";
                var durationSeconds = duration.TotalSeconds;

                _sagaStepTotalCounter.WithLabels(sagaType, stepName, serviceName).Inc();

                if (isSuccess)
                {
                    _sagaStepSuccessCounter.WithLabels(sagaType, stepName, serviceName).Inc();
                }
                else
                {
                    _sagaStepFailureCounter.WithLabels(sagaType, stepName, serviceName, "unknown").Inc();
                }

                _sagaStepDurationHistogram.WithLabels(sagaType, stepName, serviceName, status).Observe(durationSeconds);

                _logger.LogDebug("Saga step metrics: {SagaType} step {StepName} by {ServiceName} completed with status {Status} in {Duration:F3}s",
                    sagaType, stepName, serviceName, status, durationSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record saga step metrics for {SagaType} {StepName} {ServiceName}", sagaType, stepName, serviceName);
            }
        }

        public void RecordStateTransition(string sagaType, string fromState, string toState, string serviceName)
        {
            try
            {
                _stateTransitionCounter.WithLabels(sagaType, fromState, toState, serviceName).Inc();

                // Update saga by state gauge
                _sagasByStateGauge.WithLabels(sagaType, toState).Inc();
                if (fromState != "Started")
                {
                    _sagasByStateGauge.WithLabels(sagaType, fromState).Dec();
                }

                _logger.LogDebug("State transition metrics: {SagaType} transitioned from {FromState} to {ToState} by {ServiceName}",
                    sagaType, fromState, toState, serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record state transition metrics for {SagaType} {FromState} -> {ToState}", sagaType, fromState, toState);
            }
        }

        public void RecordCompensation(string sagaType, string stepName, string serviceName, bool isSuccess, TimeSpan duration)
        {
            try
            {
                var status = isSuccess ? "success" : "failure";
                var durationSeconds = duration.TotalSeconds;

                _compensationTotalCounter.WithLabels(sagaType, stepName, serviceName).Inc();

                if (isSuccess)
                {
                    _compensationSuccessCounter.WithLabels(sagaType, stepName, serviceName).Inc();
                }
                else
                {
                    _compensationFailureCounter.WithLabels(sagaType, stepName, serviceName, "unknown").Inc();
                }

                _compensationDurationHistogram.WithLabels(sagaType, stepName, serviceName, status).Observe(durationSeconds);

                _logger.LogInformation("Compensation metrics: {SagaType} compensation for step {StepName} by {ServiceName} completed with status {Status} in {Duration:F3}s",
                    sagaType, stepName, serviceName, status, durationSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record compensation metrics for {SagaType} {StepName} {ServiceName}", sagaType, stepName, serviceName);
            }
        }

        public void RecordControlledFailure(string failureType, string serviceName, string sagaId)
        {
            try
            {
                _controlledFailureCounter.WithLabels(failureType, serviceName).Inc();

                _logger.LogInformation("Controlled failure metrics: {FailureType} triggered in {ServiceName} for saga {SagaId}",
                    failureType, serviceName, sagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record controlled failure metrics for {FailureType} {ServiceName}", failureType, serviceName);
            }
        }

        public void RecordBusinessEvent(string eventType, string serviceName, string sagaId, Dictionary<string, string>? labels = null)
        {
            try
            {
                _businessEventCounter.WithLabels(eventType, serviceName).Inc();

                var labelInfo = labels != null ? string.Join(", ", labels.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "";
                _logger.LogInformation("Business event metrics: {EventType} in {ServiceName} for saga {SagaId} {LabelInfo}",
                    eventType, serviceName, sagaId, labelInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record business event metrics for {EventType} {ServiceName}", eventType, serviceName);
            }
        }

        public Dictionary<string, object> GetMetricsSummary()
        {
            try
            {
                return new Dictionary<string, object>
                {
                    ["ActiveSagas"] = _sagaStartTimes.Count,
                    ["SagaStartTimes"] = _sagaStartTimes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString("O")),
                    ["Metrics"] = new Dictionary<string, string>
                    {
                        ["SagaTotal"] = "saga_total",
                        ["SagaSuccess"] = "saga_success_total",
                        ["SagaFailure"] = "saga_failure_total",
                        ["SagaDuration"] = "saga_duration_seconds",
                        ["SagaStepTotal"] = "saga_step_total",
                        ["SagaStepSuccess"] = "saga_step_success_total",
                        ["SagaStepFailure"] = "saga_step_failure_total",
                        ["SagaStepDuration"] = "saga_step_duration_seconds",
                        ["StateTransition"] = "saga_state_transition_total",
                        ["CompensationTotal"] = "saga_compensation_total",
                        ["CompensationSuccess"] = "saga_compensation_success_total",
                        ["CompensationFailure"] = "saga_compensation_failure_total",
                        ["CompensationDuration"] = "saga_compensation_duration_seconds",
                        ["ControlledFailure"] = "controlled_failure_total",
                        ["BusinessEvent"] = "business_event_total",
                        ["ActiveSagas"] = "saga_active",
                        ["SagasByState"] = "saga_by_state"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metrics summary");
                return new Dictionary<string, object> { ["Error"] = ex.Message };
            }
        }
    }
}
