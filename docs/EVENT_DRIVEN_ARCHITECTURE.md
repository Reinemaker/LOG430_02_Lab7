# Event-Driven Architecture Implementation

## **Overview**

This document describes the implementation of an event-driven architecture for the CornerShop e-commerce platform, following the specified criteria for business scenarios, event producers, consumers, event store, CQRS, and observability.

## **1. Business Scenario Definition**

### **E-Commerce Order Processing Workflow**

**Process**: Complete order lifecycle from cart creation to delivery confirmation

**Key Events**:
- **Cart Management**: `CartCreated`, `ItemAddedToCart`, `CartCheckedOut`, `CartExpired`
- **Order Processing**: `OrderCreated`, `OrderValidated`, `OrderConfirmed`, `OrderShipped`, `OrderDelivered`
- **Payment Processing**: `PaymentInitiated`, `PaymentAuthorized`, `PaymentCompleted`, `PaymentFailed`
- **Inventory Management**: `StockReserved`, `StockReleased`, `StockUpdated`, `LowStockAlert`

**Event Flow Example**:
1. Customer adds items → `CartCreated` → `ItemAddedToCart`
2. Customer checks out → `CartCheckedOut` → `OrderCreated` → `OrderValidated`
3. Payment processing → `PaymentInitiated` → `PaymentAuthorized` → `PaymentCompleted`
4. Order fulfillment → `OrderConfirmed` → `OrderShipped` → `OrderDelivered`

## **2. Event Producers Implementation**

### **Kafka Event Publisher**

**Location**: `services/EventPublisher/KafkaEventPublisher.cs`

**Features**:
- ✅ **Idempotent publishing** with Kafka producer configuration
- ✅ **JSON serialization** with proper naming conventions
- ✅ **Event metadata** including correlation IDs, causation IDs, user context
- ✅ **Structured logging** for all published events
- ✅ **Error handling** with retry mechanisms
- ✅ **Prometheus metrics** integration

**Configuration**:
```csharp
var config = new ProducerConfig
{
    BootstrapServers = "kafka:29092",
    ClientId = "cornerShop-event-publisher",
    Acks = Acks.All,
    EnableIdempotence = true,
    MessageSendMaxRetries = 3,
    RetryBackoffMs = 1000
};
```

**Event Schema**:
```json
{
  "eventId": "uuid-v4",
  "eventType": "OrderCreated",
  "aggregateId": "order-123",
  "aggregateType": "Order",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": 1,
  "data": { /* event-specific data */ },
  "metadata": {
    "correlationId": "correlation-uuid",
    "causationId": "previous-event-uuid",
    "userId": "user-123",
    "source": "OrderService"
  }
}
```

## **3. Event Consumers Implementation**

### **Notification Service**

**Location**: `services/NotificationService/Services/NotificationService.cs`

**Features**:
- ✅ **Multiple notification types**: Email, SMS, Push notifications
- ✅ **Event-driven triggers** for business events
- ✅ **Idempotent processing** with proper error handling
- ✅ **Structured logging** for all notifications
- ✅ **Background service** for continuous event consumption

**Event Handlers**:
- `OrderCreated` → Email confirmation
- `OrderConfirmed` → Email confirmation
- `OrderShipped` → Email + SMS with tracking
- `OrderDelivered` → Email + Push notification
- `PaymentCompleted` → Email confirmation
- `PaymentFailed` → Email + SMS alert

**Consumer Configuration**:
```csharp
var config = new ConsumerConfig
{
    BootstrapServers = "kafka:29092",
    GroupId = "notification-service",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false
};
```

## **4. Event Store Implementation**

### **MongoDB Event Store**

**Location**: `services/EventStore/`

**Features**:
- ✅ **Event persistence** in MongoDB with proper indexing
- ✅ **Event replay functionality** for state reconstruction
- ✅ **Aggregate-based queries** for efficient retrieval
- ✅ **Date range queries** for analytics
- ✅ **Event statistics** and monitoring

**Stored Event Model**:
```csharp
public class StoredEvent
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string AggregateId { get; set; }
    public string AggregateType { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; }
    public string Data { get; set; }
    public string Metadata { get; set; }
    public string Topic { get; set; }
    public int Partition { get; set; }
    public long Offset { get; set; }
}
```

**Event Replay Endpoint**:
```
GET /api/eventstore/replay/{aggregateType}/{aggregateId}?fromDate={date}
```

**State Reconstruction**:
- Replays all events for an aggregate
- Reconstructs current state by applying events sequentially
- Returns both reconstructed state and event history

## **5. CQRS Implementation**

### **Command-Query Responsibility Segregation**

**Location**: `services/ReportingService/`

**Features**:
- ✅ **Read models** built from events via projections
- ✅ **Optimized queries** without impacting write operations
- ✅ **Event-driven projections** that update read models
- ✅ **MongoDB indexes** for fast query performance

**Order Read Model**:
```csharp
public class OrderReadModel
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemReadModel> Items { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentStatus { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
```

**Projection Service**:
- Consumes events from Kafka topics
- Updates read models based on event types
- Maintains consistency with event stream
- Provides fast query access to business data

**CQRS Benefits**:
- **Performance**: Read models optimized for specific queries
- **Scalability**: Separate scaling for read and write operations
- **Flexibility**: Different data models for different use cases
- **Consistency**: Event-driven synchronization between models

## **6. Observability Implementation**

### **Prometheus Metrics**

**Location**: `services/EventPublisher/Metrics/EventMetrics.cs`

**Metrics Categories**:

1. **Event Publishing Metrics**:
   - `events_published_total` - Total events published by type/topic
   - `event_publish_duration_seconds` - Publishing latency
   - `events_publish_errors_total` - Publishing errors

2. **Event Consumption Metrics**:
   - `events_consumed_total` - Total events consumed by type/topic
   - `event_consume_duration_seconds` - Processing latency
   - `events_consume_errors_total` - Consumption errors

3. **Event Store Metrics**:
   - `events_stored_total` - Events stored in event store
   - `event_store_size` - Current event count
   - `events_replayed_total` - Events replayed for state reconstruction

4. **Notification Metrics**:
   - `notifications_sent_total` - Notifications sent by type
   - `notification_send_duration_seconds` - Notification latency
   - `notification_send_errors_total` - Notification errors

5. **CQRS Metrics**:
   - `read_models_updated_total` - Read model updates
   - `read_model_update_duration_seconds` - Update latency

6. **Kafka Metrics**:
   - `kafka_consumer_lag` - Consumer lag by topic/partition
   - `kafka_producer_queue_size` - Producer queue size

### **Structured Logging**

**Features**:
- ✅ **JSON structured logs** with correlation IDs
- ✅ **Event context** in all log entries
- ✅ **Error tracking** with full stack traces
- ✅ **Performance logging** for event processing

**Log Format**:
```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "message": "Event published successfully",
  "eventType": "OrderCreated",
  "topic": "orders.events",
  "correlationId": "correlation-uuid",
  "aggregateId": "order-123"
}
```

### **Grafana Dashboards**

**Available Dashboards**:
1. **Event Flow Dashboard**: Event publishing/consumption rates
2. **Performance Dashboard**: Latency metrics and error rates
3. **Business Metrics Dashboard**: Order processing statistics
4. **Infrastructure Dashboard**: Kafka and MongoDB metrics

## **7. Infrastructure Components**

### **Kafka Setup**

**Docker Compose Configuration**:
```yaml
zookeeper:
  image: confluentinc/cp-zookeeper:7.4.0
  environment:
    ZOOKEEPER_CLIENT_PORT: 2181

kafka:
  image: confluentinc/cp-kafka:7.4.0
  environment:
    KAFKA_BROKER_ID: 1
    KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
    KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:29092
    KAFKA_AUTO_CREATE_TOPICS_ENABLE: 'true'

kafka-ui:
  image: provectuslabs/kafka-ui:latest
  ports:
    - "8081:8080"
```

**Topics**:
- `carts.events` - Cart-related events
- `orders.events` - Order-related events
- `payments.events` - Payment-related events
- `inventory.events` - Inventory-related events

### **Monitoring Stack**

**Components**:
- **Prometheus**: Metrics collection and storage
- **Grafana**: Metrics visualization and dashboards
- **Kafka UI**: Kafka topic and consumer management

## **8. Usage Examples**

### **Publishing Events**

```csharp
// In a service
var orderCreatedEvent = new OrderCreatedEvent(orderId, cartId, customerId, totalAmount, items);
await _eventPublisher.PublishAsync(orderCreatedEvent, "orders.events");
```

### **Consuming Events**

```csharp
// Automatic consumption via background service
// Events are automatically processed based on event type
```

### **Querying Read Models**

```csharp
// Fast queries without impacting write operations
var orders = await _orderReadModels.Find(o => o.Status == "Confirmed").ToListAsync();
```

### **Event Replay**

```bash
# Reconstruct order state
curl "http://localhost:5000/api/eventstore/replay/Order/order-123"
```

## **9. Benefits Achieved**

### **Scalability**
- Event-driven decoupling allows independent scaling
- Read/write separation in CQRS enables optimized scaling
- Kafka provides high-throughput message processing

### **Reliability**
- Event store provides audit trail and replay capability
- Idempotent event processing prevents duplicate handling
- Structured logging enables comprehensive debugging

### **Maintainability**
- Clear separation of concerns
- Event-driven architecture enables easy feature additions
- CQRS provides flexible data access patterns

### **Observability**
- Comprehensive metrics for monitoring
- Structured logging for debugging
- Event replay for state reconstruction

## **10. Next Steps**

### **Enhancements**
1. **Event Sourcing**: Implement full event sourcing for all aggregates
2. **Saga Pattern**: Implement distributed transaction coordination
3. **Event Versioning**: Add event schema versioning support
4. **Dead Letter Queues**: Implement failed event handling
5. **Event Encryption**: Add encryption for sensitive event data

### **Production Readiness**
1. **Security**: Add authentication and authorization
2. **Backup**: Implement event store backup strategies
3. **Monitoring**: Add alerting for critical metrics
4. **Testing**: Add comprehensive integration tests
5. **Documentation**: Add API documentation and runbooks 