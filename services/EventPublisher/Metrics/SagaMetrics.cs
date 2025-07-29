using Prometheus;

namespace EventPublisher.Metrics;

public static class SagaMetrics
{
    // Saga execution metrics
    public static readonly Counter SagasStarted = Metrics.CreateCounter(
        "sagas_started_total",
        "Total number of sagas started",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type" }
        });

    public static readonly Counter SagasCompleted = Metrics.CreateCounter(
        "sagas_completed_total",
        "Total number of sagas completed successfully",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type" }
        });

    public static readonly Counter SagasFailed = Metrics.CreateCounter(
        "sagas_failed_total",
        "Total number of sagas that failed",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "failure_reason" }
        });

    public static readonly Counter SagasCompensated = Metrics.CreateCounter(
        "sagas_compensated_total",
        "Total number of sagas that were compensated",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "compensation_reason" }
        });

    // Saga duration metrics
    public static readonly Histogram SagaExecutionDuration = Metrics.CreateHistogram(
        "saga_execution_duration_seconds",
        "Time taken to execute sagas",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type" },
            Buckets = Histogram.ExponentialBuckets(0.1, 2, 10)
        });

    public static readonly Histogram SagaCompensationDuration = Metrics.CreateHistogram(
        "saga_compensation_duration_seconds",
        "Time taken to compensate sagas",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type" },
            Buckets = Histogram.ExponentialBuckets(0.1, 2, 10)
        });

    // Saga step metrics
    public static readonly Counter SagaStepsExecuted = Metrics.CreateCounter(
        "saga_steps_executed_total",
        "Total number of saga steps executed",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "step_name", "result" }
        });

    public static readonly Histogram SagaStepExecutionDuration = Metrics.CreateHistogram(
        "saga_step_execution_duration_seconds",
        "Time taken to execute individual saga steps",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type", "step_name" },
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
        });

    public static readonly Counter SagaStepsFailed = Metrics.CreateCounter(
        "saga_steps_failed_total",
        "Total number of saga steps that failed",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "step_name", "error_type" }
        });

    // Saga compensation metrics
    public static readonly Counter SagaCompensationsExecuted = Metrics.CreateCounter(
        "saga_compensations_executed_total",
        "Total number of saga compensations executed",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "step_name", "result" }
        });

    public static readonly Histogram SagaCompensationStepDuration = Metrics.CreateHistogram(
        "saga_compensation_step_duration_seconds",
        "Time taken to execute individual compensation steps",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type", "step_name" },
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
        });

    public static readonly Counter SagaCompensationsFailed = Metrics.CreateCounter(
        "saga_compensations_failed_total",
        "Total number of saga compensations that failed",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "step_name", "error_type" }
        });

    // Saga state metrics
    public static readonly Gauge ActiveSagas = Metrics.CreateGauge(
        "active_sagas",
        "Number of currently active sagas",
        new GaugeConfiguration
        {
            LabelNames = new[] { "saga_type", "status" }
        });

    public static readonly Gauge SagaStepProgress = Metrics.CreateGauge(
        "saga_step_progress",
        "Current step progress for active sagas",
        new GaugeConfiguration
        {
            LabelNames = new[] { "saga_type", "saga_id" }
        });

    // Saga timeout metrics
    public static readonly Counter SagaTimeouts = Metrics.CreateCounter(
        "saga_timeouts_total",
        "Total number of saga timeouts",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "step_name" }
        });

    public static readonly Histogram SagaTimeoutDuration = Metrics.CreateHistogram(
        "saga_timeout_duration_seconds",
        "Time until saga timeout",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type" },
            Buckets = Histogram.ExponentialBuckets(1, 2, 10)
        });

    // Saga retry metrics
    public static readonly Counter SagaRetries = Metrics.CreateCounter(
        "saga_retries_total",
        "Total number of saga retries",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "step_name", "retry_count" }
        });

    public static readonly Histogram SagaRetryDelay = Metrics.CreateHistogram(
        "saga_retry_delay_seconds",
        "Delay between saga retries",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type", "step_name" },
            Buckets = Histogram.ExponentialBuckets(0.1, 2, 10)
        });

    // Saga event metrics
    public static readonly Counter SagaEventsPublished = Metrics.CreateCounter(
        "saga_events_published_total",
        "Total number of saga events published",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "event_type", "topic" }
        });

    public static readonly Counter SagaEventsConsumed = Metrics.CreateCounter(
        "saga_events_consumed_total",
        "Total number of saga events consumed",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "event_type", "consumer_group" }
        });

    public static readonly Histogram SagaEventProcessingDuration = Metrics.CreateHistogram(
        "saga_event_processing_duration_seconds",
        "Time taken to process saga events",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type", "event_type" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
        });

    // Saga correlation metrics
    public static readonly Counter SagaCorrelations = Metrics.CreateCounter(
        "saga_correlations_total",
        "Total number of saga correlations",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "correlation_type" }
        });

    public static readonly Histogram SagaCorrelationDuration = Metrics.CreateHistogram(
        "saga_correlation_duration_seconds",
        "Time taken to correlate saga events",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
        });

    // Saga business metrics
    public static readonly Counter OrdersProcessedBySaga = Metrics.CreateCounter(
        "orders_processed_by_saga_total",
        "Total number of orders processed by saga",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "result" }
        });

    public static readonly Histogram OrderProcessingDuration = Metrics.CreateHistogram(
        "order_processing_duration_seconds",
        "Time taken to process orders through saga",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type" },
            Buckets = Histogram.ExponentialBuckets(0.1, 2, 10)
        });

    public static readonly Gauge OrdersInProgress = Metrics.CreateGauge(
        "orders_in_progress",
        "Number of orders currently being processed",
        new GaugeConfiguration
        {
            LabelNames = new[] { "saga_type", "status" }
        });

    // Saga error metrics
    public static readonly Counter SagaErrors = Metrics.CreateCounter(
        "saga_errors_total",
        "Total number of saga errors",
        new CounterConfiguration
        {
            LabelNames = new[] { "saga_type", "error_type", "step_name" }
        });

    public static readonly Histogram SagaErrorRecoveryDuration = Metrics.CreateHistogram(
        "saga_error_recovery_duration_seconds",
        "Time taken to recover from saga errors",
        new HistogramConfiguration
        {
            LabelNames = new[] { "saga_type", "error_type" },
            Buckets = Histogram.ExponentialBuckets(0.1, 2, 10)
        });
} 