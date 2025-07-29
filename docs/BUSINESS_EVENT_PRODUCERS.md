# Business Event Producers Implementation

## Overview

This document describes the implementation of business event producers for the Corner Shop Multi-Store Management System. The implementation follows the requirements to create services that publish business events using a messaging system (Redis Streams), with proper JSON serialization, timestamps, unique IDs, and organized topics.

## Architecture

### Event Producer Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   API Layer     │    │  Event Producers │    │  Redis Streams  │
│                 │    │                  │    │                 │
│ ┌─────────────┐ │    │ ┌──────────────┐ │    │ ┌─────────────┐ │
│ │Controllers  │ │───▶│ │Order Events  │ │───▶│ │orders.events│ │
│ └─────────────┘ │    │ └──────────────┘ │    │ └─────────────┘ │
│                 │    │                  │    │                 │
│ ┌─────────────┐ │    │ ┌──────────────┐ │    │ ┌─────────────┐ │
│ │Business     │ │───▶│ │Inventory     │ │───▶│ │inventory.   │ │
│ │Events API   │ │    │ │Events        │ │    │ │events       │ │
│ └─────────────┘ │    │ └──────────────┘ │    │ └─────────────┘ │
│                 │    │                  │    │                 │
│                 │    │ ┌──────────────┐ │    │ ┌─────────────┐ │
│                 │    │ │Payment Events│ │───▶│ │payments.    │ │
│                 │    │ └──────────────┘ │    │ │events       │ │
│                 │    │                  │    │ └─────────────┘ │
│                 │    │ ┌──────────────┐ │    │ ┌─────────────┐ │
│                 │    │ │Business Event│ │───▶│ │business.    │ │
│                 │    │ │Producer      │ │    │ │events       │ │
│                 │    │ └──────────────┘ │    │ └─────────────┘ │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## Implementation Components

### 1. Event Models (`CornerShop/Models/BusinessEvents.cs`)

#### Base Event Class
```csharp
public abstract class BusinessEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string Source { get; set; } = "CornerShop.API";
    public string Version { get; set; } = "1.0";
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}
```

#### Event Types
- **Order Events**: `OrderCreated`, `OrderValidated`, `OrderAssigned`, `OrderProcessingStarted`, `OrderReadyForPickup`, `OrderCompleted`, `OrderCancelled`
- **Inventory Events**: `InventoryChecked`, `InventoryReserved`, `InventoryUpdated`, `LowStockAlert`
- **Payment Events**: `PaymentInitiated`, `PaymentCompleted`, `PaymentFailed`

### 2. Event Producer Interface (`CornerShop/Services/IBusinessEventProducer.cs`)

```csharp
public interface IBusinessEventProducer
{
    Task PublishEventAsync<T>(T businessEvent, string? correlationId = null) where T : BusinessEvent;
    Task PublishOrderEventAsync(BusinessEvent orderEvent, string? correlationId = null);
    Task PublishInventoryEventAsync(BusinessEvent inventoryEvent, string? correlationId = null);
    Task PublishPaymentEventAsync(BusinessEvent paymentEvent, string? correlationId = null);
    Task<bool> IsConnectedAsync();
    Task<EventStatistics> GetEventStatisticsAsync();
}
```

### 3. Redis Streams Implementation (`CornerShop/Services/BusinessEventProducer.cs`)

#### Key Features
- **JSON Serialization**: Events are serialized to JSON with proper naming conventions
- **Topic Organization**: Clear topic structure for different event types
- **Correlation ID Support**: Track related events across the system
- **Error Handling**: Comprehensive error handling and logging
- **Statistics Tracking**: Monitor event publishing metrics

#### Topic Structure
```
orders.events          - All order-related events
├── orders.creation    - Order creation events
├── orders.processing  - Order processing events
├── orders.completion  - Order completion events
└── orders.cancellation - Order cancellation events

inventory.events       - All inventory-related events
├── inventory.check    - Inventory check events
├── inventory.reservation - Inventory reservation events
├── inventory.update   - Inventory update events
└── inventory.alerts   - Stock alert events

payments.events        - All payment-related events
├── payments.processing - Payment processing events
├── payments.completion - Payment completion events
└── payments.failure   - Payment failure events

business.events        - All business events (main topic)
```

### 4. Specialized Event Producers

#### Order Event Producer (`CornerShop/Services/OrderEventProducer.cs`)
```csharp
public class OrderEventProducer
{
    public async Task PublishOrderCreatedAsync(string orderId, string customerId, string storeId, decimal totalAmount, List<OrderItem> items, string? correlationId = null);
    public async Task PublishOrderValidatedAsync(string orderId, bool isValid, List<string> validationErrors, string? correlationId = null);
    public async Task PublishOrderAssignedAsync(string orderId, List<StoreAssignment> assignedStores, string? correlationId = null);
    // ... other order event methods
}
```

#### Inventory Event Producer (`CornerShop/Services/InventoryEventProducer.cs`)
```csharp
public class InventoryEventProducer
{
    public async Task PublishInventoryCheckedAsync(string orderId, List<InventoryCheckResult> checkResults, string? correlationId = null);
    public async Task PublishInventoryReservedAsync(string orderId, string storeId, List<InventoryReservation> reservedItems, string? correlationId = null);
    public async Task PublishInventoryUpdatedAsync(string storeId, List<InventoryUpdate> updatedItems, string? correlationId = null);
    public async Task PublishLowStockAlertAsync(string storeId, string productId, int currentStock, int minimumStock, string? correlationId = null);
    // ... other inventory event methods
}
```

#### Payment Event Producer (`CornerShop/Services/PaymentEventProducer.cs`)
```csharp
public class PaymentEventProducer
{
    public async Task PublishPaymentInitiatedAsync(string orderId, string paymentMethod, decimal amount, string? correlationId = null);
    public async Task PublishPaymentCompletedAsync(string orderId, string transactionId, decimal amount, string paymentMethod, string? correlationId = null);
    public async Task PublishPaymentFailedAsync(string orderId, string failureReason, decimal amount, string? correlationId = null);
    // ... other payment event methods
}
```

### 5. API Controller (`CornerShop/Controllers/Api/BusinessEventsApiController.cs`)

#### Endpoints
- `GET /api/v1/BusinessEvents/statistics` - Get event statistics
- `GET /api/v1/BusinessEvents/connection-status` - Check connection status
- `POST /api/v1/BusinessEvents/demo/order` - Demo order events
- `POST /api/v1/BusinessEvents/demo/inventory` - Demo inventory events
- `POST /api/v1/BusinessEvents/demo/payment` - Demo payment events
- `POST /api/v1/BusinessEvents/demo/complete-process` - Demo complete business process

## Usage Examples

### 1. Publishing Order Events

```csharp
// Inject the order event producer
private readonly OrderEventProducer _orderEventProducer;

// Publish order created event
await _orderEventProducer.PublishOrderCreatedAsync(
    orderId: "order-123",
    customerId: "customer-456",
    storeId: "store-001",
    totalAmount: 99.99m,
    items: orderItems,
    correlationId: "correlation-guid"
);
```

### 2. Publishing Inventory Events

```csharp
// Inject the inventory event producer
private readonly InventoryEventProducer _inventoryEventProducer;

// Publish inventory checked event
await _inventoryEventProducer.PublishInventoryCheckedAsync(
    orderId: "order-123",
    checkResults: inventoryResults,
    correlationId: "correlation-guid"
);
```

### 3. Publishing Payment Events

```csharp
// Inject the payment event producer
private readonly PaymentEventProducer _paymentEventProducer;

// Publish payment completed event
await _paymentEventProducer.PublishPaymentCompletedAsync(
    orderId: "order-123",
    transactionId: "txn-789",
    amount: 99.99m,
    paymentMethod: "credit_card",
    correlationId: "correlation-guid"
);
```

## Event Structure

### JSON Event Format
```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "eventType": "OrderCreated",
  "timestamp": "2024-01-01T12:00:00.000Z",
  "correlationId": "correlation-guid",
  "source": "CornerShop.API",
  "version": "1.0",
  "data": {
    "orderId": "order-123",
    "customerId": "customer-456",
    "storeId": "store-001",
    "totalAmount": 99.99,
    "items": [
      {
        "productId": "prod-001",
        "quantity": 2,
        "unitPrice": 49.99,
        "totalPrice": 99.98
      }
    ]
  }
}
```

### Redis Stream Entry
```
orders.creation:1704110400000-0
eventId: 550e8400-e29b-41d4-a716-446655440000
eventType: OrderCreated
timestamp: 2024-01-01T12:00:00.000Z
correlationId: correlation-guid
source: CornerShop.API
version: 1.0
data: {"orderId":"order-123","customerId":"customer-456",...}
```

## Testing

### Running the Test Suite

```bash
# Make the test script executable
chmod +x test-business-event-producers.sh

# Run the test suite
./test-business-event-producers.sh
```

### Test Coverage

The test suite covers:
- ✅ Connection status verification
- ✅ Event statistics monitoring
- ✅ Order event publishing
- ✅ Inventory event publishing
- ✅ Payment event publishing
- ✅ Complete business process demo
- ✅ Performance testing
- ✅ API endpoint validation

### Manual Testing

```bash
# Test connection status
curl -X GET "http://cornershop.localhost/api/v1/BusinessEvents/connection-status"

# Test event statistics
curl -X GET "http://cornershop.localhost/api/v1/BusinessEvents/statistics"

# Demo order events
curl -X POST "http://cornershop.localhost/api/v1/BusinessEvents/demo/order" \
  -H "Content-Type: application/json" \
  -d '{"orderId": "test-order-001", "customerId": "customer-123"}'
```

## Configuration

### Dependency Injection

```csharp
// Program.cs
builder.Services.AddSingleton<IBusinessEventProducer, BusinessEventProducer>();
builder.Services.AddScoped<OrderEventProducer>();
builder.Services.AddScoped<InventoryEventProducer>();
builder.Services.AddScoped<PaymentEventProducer>();
```

### Redis Configuration

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

## Monitoring and Observability

### Event Statistics

The system tracks:
- Total events published
- Events by category (orders, inventory, payments, stores)
- Events by type
- Last event published timestamp

### Health Checks

```bash
# Check event producer health
curl -X GET "http://cornershop.localhost/api/v1/BusinessEvents/connection-status"
```

### Logging

Events are logged with structured logging:
```csharp
_logger.LogInformation("Event published successfully: {EventType} | {EventId} | {Topic} | {EntryId}",
    businessEvent.EventType, businessEvent.EventId, topic, entryId);
```

## Integration with Existing System

### Saga Orchestration Integration

The event producers integrate with the existing saga orchestration system:

```csharp
// In saga orchestrator
await _orderEventProducer.PublishOrderCreatedAsync(orderId, customerId, storeId, totalAmount, items, sagaId);
await _inventoryEventProducer.PublishInventoryReservedAsync(orderId, storeId, reservedItems, sagaId);
await _paymentEventProducer.PublishPaymentCompletedAsync(orderId, transactionId, amount, paymentMethod, sagaId);
```

### Business Event Logging

Events are also logged through the existing business event logger:

```csharp
_businessEventLogger.LogBusinessEvent("OrderCreated", sagaId, "OrderService", eventData);
```

## Best Practices

### 1. Correlation IDs
Always use correlation IDs to track related events across the system:
```csharp
var correlationId = Guid.NewGuid().ToString();
await _orderEventProducer.PublishOrderCreatedAsync(orderId, customerId, storeId, totalAmount, items, correlationId);
```

### 2. Error Handling
Implement proper error handling for event publishing:
```csharp
try
{
    await _eventProducer.PublishEventAsync(businessEvent, correlationId);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to publish event: {EventType}", businessEvent.EventType);
    // Handle the error appropriately
}
```

### 3. Event Versioning
Use versioning for event schema evolution:
```csharp
public string Version { get; set; } = "1.0";
```

### 4. Topic Organization
Organize topics logically for easy filtering and consumption:
```
orders.events          - All order events
orders.creation        - Order creation only
orders.processing      - Order processing only
```

## Future Enhancements

### 1. Event Consumers
- Implement event consumers for processing published events
- Add event-driven workflows
- Implement event sourcing

### 2. Event Persistence
- Add event store for long-term event retention
- Implement event replay capabilities
- Add event archiving

### 3. Event Filtering
- Add event filtering by type, source, or correlation ID
- Implement event routing based on content
- Add event transformation capabilities

### 4. Event Versioning
- Implement event schema versioning
- Add backward compatibility support
- Implement event migration tools

### 5. Monitoring and Alerting
- Add real-time event monitoring dashboards
- Implement event-based alerting
- Add event performance metrics

## Conclusion

The business event producers implementation provides a robust foundation for event-driven architecture in the Corner Shop system. It follows best practices for event publishing, includes comprehensive testing, and integrates seamlessly with the existing system architecture.

The implementation satisfies all the requirements:
- ✅ Created services that publish business events
- ✅ Used Redis Streams as the messaging system
- ✅ Serialized events in JSON with timestamps and unique IDs
- ✅ Organized topics clearly (orders.events, inventory.events, etc.)
- ✅ Provided comprehensive testing and documentation 