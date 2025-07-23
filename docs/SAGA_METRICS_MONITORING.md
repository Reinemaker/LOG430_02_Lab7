# Saga Metrics, Monitoring & Structured Logging

## Overview

The CornerShop system includes comprehensive metrics collection, monitoring capabilities, and structured logging for saga orchestration. This implementation provides real-time visibility into saga execution, state evolution, and business events.

## Prometheus Metrics

### Saga Execution Metrics

#### Counters
- **`saga_total`**: Total number of sagas by type
  - Labels: `saga_type`
- **`saga_success_total`**: Total successful sagas
  - Labels: `saga_type`
- **`saga_failure_total`**: Total failed sagas
  - Labels: `saga_type`, `failure_reason`

#### Histograms
- **`saga_duration_seconds`**: Saga execution duration
  - Labels: `saga_type`, `status`
  - Buckets: 0.1, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 60.0

#### Gauges
- **`saga_active`**: Currently active sagas
  - Labels: `saga_type`
- **`saga_by_state`**: Number of sagas by state
  - Labels: `saga_type`, `state`

### Saga Step Metrics

#### Counters
- **`saga_step_total`**: Total saga steps
  - Labels: `saga_type`, `step_name`, `service_name`
- **`saga_step_success_total`**: Successful saga steps
  - Labels: `saga_type`, `step_name`, `service_name`
- **`saga_step_failure_total`**: Failed saga steps
  - Labels: `saga_type`, `step_name`, `service_name`, `error_type`

#### Histograms
- **`saga_step_duration_seconds`**: Step execution duration
  - Labels: `saga_type`, `step_name`, `service_name`, `status`
  - Buckets: 0.01, 0.05, 0.1, 0.5, 1.0, 2.0, 5.0

### State Transition Metrics

#### Counters
- **`saga_state_transition_total`**: State transitions
  - Labels: `saga_type`, `from_state`, `to_state`, `service_name`

### Compensation Metrics

#### Counters
- **`saga_compensation_total`**: Total compensation actions
  - Labels: `saga_type`, `step_name`, `service_name`
- **`saga_compensation_success_total`**: Successful compensations
  - Labels: `saga_type`, `step_name`, `service_name`
- **`saga_compensation_failure_total`**: Failed compensations
  - Labels: `saga_type`, `step_name`, `service_name`, `error_type`

#### Histograms
- **`saga_compensation_duration_seconds`**: Compensation duration
  - Labels: `saga_type`, `step_name`, `service_name`, `status`
  - Buckets: 0.01, 0.05, 0.1, 0.5, 1.0, 2.0, 5.0

### Controlled Failure Metrics

#### Counters
- **`controlled_failure_total`**: Controlled failures
  - Labels: `failure_type`, `service_name`

### Business Event Metrics

#### Counters
- **`business_event_total`**: Business events
  - Labels: `event_type`, `service_name`

## Grafana Dashboard

### Dashboard Configuration

The system includes a comprehensive Grafana dashboard (`saga-monitoring.json`) with the following panels:

#### 1. Saga Execution Overview
- **Panel Type**: Stat
- **Metrics**: `saga_total`
- **Purpose**: Display total sagas by type

#### 2. Saga Success Rate
- **Panel Type**: Stat
- **Metrics**: `rate(saga_success_total[5m]) / rate(saga_total[5m]) * 100`
- **Purpose**: Real-time success rate monitoring
- **Thresholds**: Red (80%), Yellow (95%), Green (99%)

#### 3. Active Sagas
- **Panel Type**: Stat
- **Metrics**: `saga_active`
- **Purpose**: Monitor currently active sagas
- **Thresholds**: Green (0), Yellow (10), Red (20)

#### 4. Saga Duration Distribution
- **Panel Type**: Heatmap
- **Metrics**: `rate(saga_duration_seconds_bucket[5m])`
- **Purpose**: Visualize duration patterns

#### 5. State Transitions Over Time
- **Panel Type**: Time Series
- **Metrics**: `rate(saga_state_transition_total[5m])`
- **Purpose**: Track state evolution over time

#### 6. Sagas by State
- **Panel Type**: Pie Chart
- **Metrics**: `saga_by_state`
- **Purpose**: Distribution of sagas by current state

#### 7. Step Execution Success Rate
- **Panel Type**: Stat
- **Metrics**: `rate(saga_step_success_total[5m]) / rate(saga_step_total[5m]) * 100`
- **Purpose**: Monitor step-level success rates

#### 8. Compensation Success Rate
- **Panel Type**: Stat
- **Metrics**: `rate(saga_compensation_success_total[5m]) / rate(saga_compensation_total[5m]) * 100`
- **Purpose**: Monitor compensation effectiveness

#### 9. Controlled Failures
- **Panel Type**: Time Series
- **Metrics**: `rate(controlled_failure_total[5m])`
- **Purpose**: Track controlled failure patterns

#### 10. Business Events
- **Panel Type**: Time Series
- **Metrics**: `rate(business_event_total[5m])`
- **Purpose**: Monitor business event frequency

#### 11. Step Duration Percentiles
- **Panel Type**: Time Series
- **Metrics**: 
  - `histogram_quantile(0.95, rate(saga_step_duration_seconds_bucket[5m]))`
  - `histogram_quantile(0.50, rate(saga_step_duration_seconds_bucket[5m]))`
- **Purpose**: Performance analysis

#### 12. Saga Duration Percentiles
- **Panel Type**: Time Series
- **Metrics**:
  - `histogram_quantile(0.95, rate(saga_duration_seconds_bucket[5m]))`
  - `histogram_quantile(0.50, rate(saga_duration_seconds_bucket[5m]))`
- **Purpose**: Overall performance analysis

### Dashboard Features

- **Auto-refresh**: 30-second intervals
- **Time Range**: Last 1 hour by default
- **Dark Theme**: Optimized for monitoring
- **Responsive Layout**: Adapts to different screen sizes
- **Interactive Elements**: Drill-down capabilities

## Structured Logging

### Business Event Logging

The system implements structured logging for all business events and decisions:

#### Event Types

1. **Saga Lifecycle Events**
   - `lifecycle_started`: Saga initiation
   - `lifecycle_completed`: Successful saga completion
   - `lifecycle_failed`: Saga failure

2. **Step Events**
   - `step_completed`: Step execution success
   - `step_failed`: Step execution failure

3. **State Transition Events**
   - `state_transition`: State machine transitions

4. **Compensation Events**
   - `compensation`: Compensation action execution

5. **Controlled Failure Events**
   - `controlled_failure`: Simulated failure events

#### Log Format

```json
{
  "timestamp": "2024-01-15T10:30:00.000Z",
  "event_type": "step_completed",
  "saga_id": "saga_123",
  "service_name": "ProductService",
  "correlation_id": "saga_123",
  "message": "Business event step_completed occurred",
  "data": {
    "step_name": "ValidateStockAvailability",
    "duration_seconds": 0.125,
    "step_data": { ... }
  },
  "category": "business_event",
  "severity": "info",
  "environment": "Development",
  "version": "1.0.0"
}
```

### Business Decision Logging

Decisions made during saga execution are logged with context:

```json
{
  "timestamp": "2024-01-15T10:30:00.000Z",
  "event_type": "stock_validation",
  "saga_id": "saga_123",
  "service_name": "ProductService",
  "correlation_id": "saga_123",
  "message": "Stock validation decision",
  "data": {
    "product_name": "Coffee",
    "requested_quantity": 5,
    "available_stock": 10,
    "decision": "approved"
  },
  "category": "business_decision",
  "severity": "info",
  "decision_reason": "Sufficient stock available",
  "environment": "Development",
  "version": "1.0.0"
}
```

## API Endpoints

### Metrics API (`/api/SagaMetrics`)

#### GET /api/SagaMetrics/summary
Get comprehensive metrics summary.

**Response:**
```json
{
  "success": true,
  "summary": {
    "activeSagas": 2,
    "sagaStartTimes": { ... },
    "metrics": {
      "SagaTotal": "saga_total",
      "SagaSuccess": "saga_success_total",
      "SagaFailure": "saga_failure_total",
      "SagaDuration": "saga_duration_seconds",
      "SagaStepTotal": "saga_step_total",
      "SagaStepSuccess": "saga_step_success_total",
      "SagaStepFailure": "saga_step_failure_total",
      "SagaStepDuration": "saga_step_duration_seconds",
      "StateTransition": "saga_state_transition_total",
      "CompensationTotal": "saga_compensation_total",
      "CompensationSuccess": "saga_compensation_success_total",
      "CompensationFailure": "saga_compensation_failure_total",
      "CompensationDuration": "saga_compensation_duration_seconds",
      "ControlledFailure": "controlled_failure_total",
      "BusinessEvent": "business_event_total",
      "ActiveSagas": "saga_active",
      "SagasByState": "saga_by_state"
    }
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/SagaMetrics/prometheus
Get Prometheus metrics in text format.

**Response:** Prometheus text format metrics

#### GET /api/SagaMetrics/performance
Get saga performance statistics.

**Response:**
```json
{
  "success": true,
  "performance": {
    "totalSagas": 25,
    "completedSagas": 20,
    "failedSagas": 5,
    "successRate": 0.8,
    "failureRate": 0.2,
    "averageTransitions": 4.2,
    "recentSagas": 8,
    "recentCompletions": 6,
    "recentFailures": 2
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/SagaMetrics/state-distribution
Get saga state distribution.

**Response:**
```json
{
  "success": true,
  "stateDistribution": [
    {
      "state": "Completed",
      "count": 15,
      "percentage": 60.0
    },
    {
      "state": "Failed",
      "count": 5,
      "percentage": 20.0
    },
    {
      "state": "Compensated",
      "count": 3,
      "percentage": 12.0
    },
    {
      "state": "Active",
      "count": 2,
      "percentage": 8.0
    }
  ],
  "totalSagas": 25,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/SagaMetrics/transition-analysis
Get saga transition analysis.

**Response:**
```json
{
  "success": true,
  "transitions": [
    {
      "fromState": "Started",
      "toState": "StoreValidated",
      "count": 25,
      "averageDuration": 0.15
    },
    {
      "fromState": "StoreValidated",
      "toState": "StockReserved",
      "count": 20,
      "averageDuration": 0.25
    },
    {
      "fromState": "StockReserved",
      "toState": "Failed",
      "count": 5,
      "averageDuration": 0.10
    }
  ],
  "totalTransitions": 50,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/SagaMetrics/duration-stats
Get saga duration statistics.

**Response:**
```json
{
  "success": true,
  "durationStats": {
    "averageDuration": 2.5,
    "minDuration": 0.8,
    "maxDuration": 8.2,
    "medianDuration": 2.1,
    "totalCompleted": 20
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/SagaMetrics/recent-activity
Get recent saga activity.

**Response:**
```json
{
  "success": true,
  "recentSagas": [
    {
      "sagaId": "saga_123",
      "sagaType": "SaleSaga",
      "currentState": "Completed",
      "createdAt": "2024-01-15T10:25:00Z",
      "completedAt": "2024-01-15T10:25:03Z",
      "duration": 3.2,
      "transitionCount": 5,
      "lastTransition": "2024-01-15T10:25:03Z"
    }
  ],
  "timeWindow": "1 hour(s)",
  "totalInWindow": 8,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/SagaMetrics/grafana
Get Grafana integration information.

**Response:**
```json
{
  "success": true,
  "grafanaIntegration": {
    "metrics": {
      "SagaTotal": "saga_total",
      "SagaSuccess": "saga_success_total",
      "SagaFailure": "saga_failure_total",
      "SagaDuration": "saga_duration_seconds",
      "SagaStepTotal": "saga_step_total",
      "SagaStepSuccess": "saga_step_success_total",
      "SagaStepFailure": "saga_step_failure_total",
      "SagaStepDuration": "saga_step_duration_seconds",
      "StateTransition": "saga_state_transition_total",
      "CompensationTotal": "saga_compensation_total",
      "CompensationSuccess": "saga_compensation_success_total",
      "CompensationFailure": "saga_compensation_failure_total",
      "CompensationDuration": "saga_compensation_duration_seconds",
      "ControlledFailure": "controlled_failure_total",
      "BusinessEvent": "business_event_total",
      "ActiveSagas": "saga_active",
      "SagasByState": "saga_by_state"
    },
    "prometheusEndpoint": "/api/SagaMetrics/prometheus",
    "dashboardUrl": "/grafana/d/saga-monitoring/saga-orchestration-monitoring"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Implementation Details

### Metrics Service Integration

The `SagaMetricsService` integrates with all saga components:

```csharp
// Record saga start
_metricsService.RecordSagaStart("SaleSaga", sagaId);

// Record saga completion
_metricsService.RecordSagaCompletion("SaleSaga", sagaId, true, duration);

// Record step execution
_metricsService.RecordSagaStep("SaleSaga", "ValidateStock", "ProductService", true, stepDuration);

// Record state transition
_metricsService.RecordStateTransition("SaleSaga", "Started", "StoreValidated", "StoreService");

// Record compensation
_metricsService.RecordCompensation("SaleSaga", "StockReservation", "ProductService", true, compensationDuration);

// Record controlled failure
_metricsService.RecordControlledFailure("insufficient_stock", "ProductService", sagaId);

// Record business event
_metricsService.RecordBusinessEvent("step_completed", "ProductService", sagaId, eventData);
```

### Structured Logging Integration

The `BusinessEventLogger` provides structured logging:

```csharp
// Log business event
_businessLogger.LogBusinessEvent("step_completed", sagaId, "ProductService", data);

// Log business decision
_businessLogger.LogBusinessDecision("stock_validation", sagaId, "ProductService", data, "Sufficient stock");

// Log saga lifecycle
_businessLogger.LogSagaLifecycle("started", sagaId, "SaleSaga", lifecycleData);

// Log state transition
_businessLogger.LogStateTransition(sagaId, "SaleSaga", "Started", "StoreValidated", "StoreService");

// Log compensation
_businessLogger.LogCompensation(sagaId, "SaleSaga", "StockReservation", "ProductService", true);

// Log controlled failure
_businessLogger.LogControlledFailure(sagaId, "insufficient_stock", "ProductService", failureData);
```

## Benefits

### 1. Real-time Monitoring
- **Live Metrics**: Real-time saga execution metrics
- **State Evolution**: Visualize state transitions over time
- **Performance Tracking**: Monitor duration and success rates
- **Failure Analysis**: Track and analyze failures

### 2. Operational Insights
- **Success Rates**: Monitor saga and step success rates
- **Performance Trends**: Track performance over time
- **Bottleneck Identification**: Identify slow steps or services
- **Compensation Effectiveness**: Monitor compensation success rates

### 3. Business Intelligence
- **Structured Logging**: JSON-formatted business events
- **Decision Tracking**: Log business decisions with context
- **Correlation**: Link events through correlation IDs
- **Audit Trail**: Complete audit trail of saga execution

### 4. Alerting and Notifications
- **Threshold Monitoring**: Set alerts on key metrics
- **Failure Detection**: Automatic failure detection
- **Performance Alerts**: Alert on performance degradation
- **Business Event Alerts**: Alert on important business events

### 5. Debugging and Troubleshooting
- **Detailed Logs**: Structured logs for debugging
- **Metrics Correlation**: Correlate metrics with logs
- **State Tracking**: Track state evolution for debugging
- **Performance Analysis**: Analyze performance bottlenecks

## Best Practices

### 1. Metrics Collection
- Collect metrics at appropriate granularity
- Use meaningful labels for filtering and grouping
- Set appropriate histogram buckets for duration metrics
- Monitor metric cardinality to avoid performance issues

### 2. Dashboard Design
- Design dashboards for different audiences (operators, developers, business)
- Use appropriate visualization types for different metrics
- Set meaningful thresholds and alerts
- Include drill-down capabilities for detailed analysis

### 3. Logging Strategy
- Use structured logging for all business events
- Include correlation IDs for tracing
- Log appropriate context data
- Use appropriate log levels

### 4. Performance Monitoring
- Monitor metrics collection overhead
- Set up alerts for metric collection failures
- Regularly review and optimize metric queries
- Monitor storage requirements for metrics and logs

## Conclusion

The saga metrics, monitoring, and structured logging implementation provides comprehensive visibility into saga orchestration. It enables:

- Real-time monitoring of saga execution
- Visualization of state evolution through Grafana
- Structured logging of business events and decisions
- Performance analysis and optimization
- Operational troubleshooting and debugging
- Business intelligence and insights

This implementation satisfies the requirements for Prometheus metrics tracking, Grafana visualization, and structured business event logging, providing a complete observability solution for saga orchestration. 