using Prometheus;

namespace EventPublisher.Metrics;

public static class EventMetrics
{
    // Event publishing metrics
    public static readonly Counter EventsPublished = Metrics.CreateCounter(
        "events_published_total",
        "Total number of events published",
        new CounterConfiguration
        {
            LabelNames = new[] { "event_type", "topic", "source" }
        });

    public static readonly Histogram EventPublishDuration = Metrics.CreateHistogram(
        "event_publish_duration_seconds",
        "Time taken to publish events",
        new HistogramConfiguration
        {
            LabelNames = new[] { "event_type", "topic" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
        });

    public static readonly Counter EventsPublishErrors = Metrics.CreateCounter(
        "events_publish_errors_total",
        "Total number of event publishing errors",
        new CounterConfiguration
        {
            LabelNames = new[] { "event_type", "topic", "error_type" }
        });

    // Event consumption metrics
    public static readonly Counter EventsConsumed = Metrics.CreateCounter(
        "events_consumed_total",
        "Total number of events consumed",
        new CounterConfiguration
        {
            LabelNames = new[] { "event_type", "topic", "consumer_group" }
        });

    public static readonly Histogram EventConsumeDuration = Metrics.CreateHistogram(
        "event_consume_duration_seconds",
        "Time taken to process consumed events",
        new HistogramConfiguration
        {
            LabelNames = new[] { "event_type", "topic", "consumer_group" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
        });

    public static readonly Counter EventsConsumeErrors = Metrics.CreateCounter(
        "events_consume_errors_total",
        "Total number of event consumption errors",
        new CounterConfiguration
        {
            LabelNames = new[] { "event_type", "topic", "consumer_group", "error_type" }
        });

    // Event store metrics
    public static readonly Counter EventsStored = Metrics.CreateCounter(
        "events_stored_total",
        "Total number of events stored in event store",
        new CounterConfiguration
        {
            LabelNames = new[] { "event_type", "aggregate_type" }
        });

    public static readonly Gauge EventStoreSize = Metrics.CreateGauge(
        "event_store_size",
        "Current number of events in the event store",
        new GaugeConfiguration
        {
            LabelNames = new[] { "aggregate_type" }
        });

    // Event replay metrics
    public static readonly Counter EventsReplayed = Metrics.CreateCounter(
        "events_replayed_total",
        "Total number of events replayed",
        new CounterConfiguration
        {
            LabelNames = new[] { "aggregate_type", "aggregate_id" }
        });

    public static readonly Histogram EventReplayDuration = Metrics.CreateHistogram(
        "event_replay_duration_seconds",
        "Time taken to replay events",
        new HistogramConfiguration
        {
            LabelNames = new[] { "aggregate_type" },
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
        });

    // Notification metrics
    public static readonly Counter NotificationsSent = Metrics.CreateCounter(
        "notifications_sent_total",
        "Total number of notifications sent",
        new CounterConfiguration
        {
            LabelNames = new[] { "notification_type", "event_type" }
        });

    public static readonly Histogram NotificationSendDuration = Metrics.CreateHistogram(
        "notification_send_duration_seconds",
        "Time taken to send notifications",
        new HistogramConfiguration
        {
            LabelNames = new[] { "notification_type" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
        });

    public static readonly Counter NotificationSendErrors = Metrics.CreateCounter(
        "notification_send_errors_total",
        "Total number of notification sending errors",
        new CounterConfiguration
        {
            LabelNames = new[] { "notification_type", "error_type" }
        });

    // CQRS metrics
    public static readonly Counter ReadModelsUpdated = Metrics.CreateCounter(
        "read_models_updated_total",
        "Total number of read model updates",
        new CounterConfiguration
        {
            LabelNames = new[] { "read_model_type", "event_type" }
        });

    public static readonly Histogram ReadModelUpdateDuration = Metrics.CreateHistogram(
        "read_model_update_duration_seconds",
        "Time taken to update read models",
        new HistogramConfiguration
        {
            LabelNames = new[] { "read_model_type" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
        });

    // Kafka metrics
    public static readonly Gauge KafkaConsumerLag = Metrics.CreateGauge(
        "kafka_consumer_lag",
        "Consumer lag for Kafka topics",
        new GaugeConfiguration
        {
            LabelNames = new[] { "topic", "partition", "consumer_group" }
        });

    public static readonly Gauge KafkaProducerQueueSize = Metrics.CreateGauge(
        "kafka_producer_queue_size",
        "Number of messages in producer queue",
        new GaugeConfiguration
        {
            LabelNames = new[] { "topic" }
        });
} 