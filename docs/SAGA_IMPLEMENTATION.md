# Saga Pattern Implementation - CornerShop E-Commerce

## **Overview**

This document describes the implementation of the Saga pattern (choreographed) for the CornerShop e-commerce platform, providing distributed transaction coordination with compensation capabilities.

## **1. Analysis of Existing Business Scenario**

### **Current E-Commerce Order Processing Workflow**

The existing business scenario involves multiple services that need to coordinate in a distributed transaction:

#### **Services Involved:**
1. **CartService** - Manages shopping cart
2. **OrderService** - Creates and manages orders
3. **StockService** - Manages inventory
4. **PaymentService** - Processes payments
5. **NotificationService** - Sends notifications
6. **ReportingService** - Updates read models

#### **Problem with Current Flow:**
- **No rollback mechanism** if any step fails
- **Inconsistent state** possible across services
- **No compensation** for partial failures
- **Difficult to maintain** data consistency

## **2. Saga Choreography Design**

### **Distributed Transaction Flow**

The Saga pattern coordinates the order processing as a distributed transaction with compensation capabilities.

#### **Saga Steps:**
1. **Create Order** (OrderService) - Step 1
2. **Reserve Stock** (StockService) - Step 2
3. **Process Payment** (PaymentService) - Step 3
4. **Confirm Order** (OrderService) - Step 4
5. **Send Notifications** (NotificationService) - Step 5

#### **Compensation Steps:**
1. **Cancel Order** (OrderService) - Compensates Create Order
2. **Release Stock** (StockService) - Compensates Reserve Stock
3. **Refund Payment** (PaymentService) - Compensates Process Payment
4. **Send Failure Notifications** (NotificationService) - Compensates Send Notifications

### **Saga Events Definition**

#### **Initiating Events:**
- `OrderSagaStarted` - Initiates the distributed transaction

#### **Success Events:**
- `OrderSagaStepCompleted` - Each step completion
- `OrderSagaCompleted` - Entire saga successful

#### **Compensation Events:**
- `OrderSagaStepFailed` - Individual step failure
- `OrderSagaCompensationStarted` - Compensation initiated
- `OrderSagaCompensationCompleted` - Compensation completed
- `OrderSagaFailed` - Entire saga failed

### **Saga State Management**

#### **Saga States:**
- `Started` - Saga initiated
- `InProgress` - Steps being executed
- `Completed` - All steps successful
- `Compensating` - Compensation in progress
- `Failed` - Saga failed with compensation
- `Aborted` - Saga aborted without compensation

## **3. Technical Implementation**

### **Saga Events**

**Location**: `shared/CornerShop.Shared/Events/SagaEvents.cs`

**Key Events**:
- `OrderSagaStartedEvent` - Initiates saga
- `OrderSagaStepCompletedEvent` - Step completion
- `OrderSagaStepFailedEvent` - Step failure
- `OrderSagaCompensationStartedEvent` - Compensation start
- `OrderSagaCompensationCompletedEvent` - Compensation completion
- `OrderSagaCompletedEvent` - Saga success
- `OrderSagaFailedEvent` - Saga failure

**Event Schema**:
```json
{
  "eventId": "uuid-v4",
  "eventType": "OrderSagaStarted",
  "aggregateId": "saga-uuid",
  "aggregateType": "Saga",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": 1,
  "data": {
    "sagaId": "saga-uuid",
    "orderId": "order-123",
    "customerId": "customer-456",
    "totalAmount": 150.00,
    "items": [...]
  },
  "metadata": {
    "correlationId": "correlation-uuid",
    "sagaId": "saga-uuid",
    "step": 1,
    "totalSteps": 5
  }
}
```

### **Saga State Management**

**Location**: `shared/CornerShop.Shared/Models/SagaState.cs`

**Key Components**:
- `SagaState` - Main saga state model
- `SagaStep` - Individual step tracking
- `SagaStatus` - Saga status enumeration
- `SagaStepStatus` - Step status enumeration
- `SagaSteps` - Step definitions and mappings

**State Model**:
```csharp
public class SagaState
{
    public string SagaId { get; set; }
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public SagaStatus Status { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public int? FailedStep { get; set; }
    public string? FailureReason { get; set; }
    public List<SagaStep> Steps { get; set; }
    public Dictionary<string, object> CompensationData { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
}
```

### **Saga Orchestrator Service**

**Location**: `services/SagaOrchestrator/Services/SagaOrchestratorService.cs`

**Features**:
- ✅ **Saga lifecycle management** - Start, complete, fail sagas
- ✅ **Step tracking** - Monitor individual step progress
- ✅ **Compensation coordination** - Handle rollback scenarios
- ✅ **State persistence** - MongoDB-based state storage
- ✅ **Event publishing** - Kafka event integration
- ✅ **Error handling** - Comprehensive error management

**Key Methods**:
- `StartOrderSagaAsync()` - Initialize new saga
- `HandleSagaStepCompletedAsync()` - Process step completion
- `HandleSagaStepFailedAsync()` - Process step failure
- `HandleSagaCompensationCompletedAsync()` - Process compensation
- `GetSagaStateAsync()` - Retrieve saga state
- `GetSagaStatesByOrderIdAsync()` - Query sagas by order

## **4. Saga Metrics and Observability**

### **Prometheus Metrics**

**Location**: `services/EventPublisher/Metrics/SagaMetrics.cs`

**Metrics Categories**:

#### **Saga Execution Metrics:**
- `sagas_started_total` - Total sagas started by type
- `sagas_completed_total` - Total sagas completed successfully
- `sagas_failed_total` - Total sagas that failed
- `sagas_compensated_total` - Total sagas that were compensated

#### **Saga Duration Metrics:**
- `saga_execution_duration_seconds` - Saga execution time
- `saga_compensation_duration_seconds` - Compensation time
- `saga_step_execution_duration_seconds` - Individual step time

#### **Saga Step Metrics:**
- `saga_steps_executed_total` - Steps executed by type/result
- `saga_steps_failed_total` - Failed steps by type/error
- `saga_compensations_executed_total` - Compensations executed

#### **Saga State Metrics:**
- `active_sagas` - Currently active sagas by status
- `saga_step_progress` - Step progress for active sagas
- `orders_in_progress` - Orders being processed

#### **Saga Error Metrics:**
- `saga_errors_total` - Total saga errors by type
- `saga_error_recovery_duration_seconds` - Error recovery time
- `saga_timeouts_total` - Saga timeouts by step

### **Structured Logging**

**Features**:
- ✅ **Saga correlation IDs** - Track saga across services
- ✅ **Step-level logging** - Detailed step execution logs
- ✅ **Compensation logging** - Rollback operation logs
- ✅ **Error context** - Comprehensive error information

**Log Format**:
```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "message": "Saga step completed",
  "sagaId": "saga-uuid",
  "stepName": "ProcessPayment",
  "stepNumber": 3,
  "correlationId": "correlation-uuid",
  "orderId": "order-123"
}
```

## **5. Testing and Simulation**

### **Saga Test Controller**

**Location**: `services/SagaOrchestrator/Controllers/SagaTestController.cs`

**Test Endpoints**:

#### **Success Scenarios:**
- `POST /api/sagatest/start-success-saga` - Start successful saga
- `POST /api/sagatest/run-complete-success-scenario` - Complete success flow

#### **Failure Scenarios:**
- `POST /api/sagatest/start-failure-saga` - Start failing saga
- `POST /api/sagatest/run-complete-failure-scenario` - Complete failure flow

#### **Step Simulation:**
- `POST /api/sagatest/simulate-step-completion` - Simulate step success
- `POST /api/sagatest/simulate-step-failure` - Simulate step failure
- `POST /api/sagatest/simulate-compensation-completion` - Simulate compensation

#### **State Queries:**
- `GET /api/sagatest/saga-state/{sagaId}` - Get saga state
- `GET /api/sagatest/saga-states/order/{orderId}` - Get sagas by order

### **Test Scenarios**

#### **Complete Success Scenario:**
1. Start saga with valid order data
2. Execute all 5 steps successfully
3. Verify saga completion
4. Check final state and metrics

#### **Complete Failure Scenario:**
1. Start saga with failing payment data
2. Execute first 2 steps successfully
3. Fail at payment step (step 3)
4. Trigger compensation for steps 2 and 1
5. Verify saga failure and compensation
6. Check compensation metrics

## **6. Grafana Integration**

### **Saga Dashboards**

**Available Dashboards**:

#### **Saga Overview Dashboard:**
- Total sagas started/completed/failed
- Active sagas by status
- Saga execution duration trends
- Success/failure rates

#### **Saga Step Dashboard:**
- Step execution counts by type
- Step failure rates
- Step execution duration
- Compensation statistics

#### **Saga Business Dashboard:**
- Orders processed by saga
- Order processing duration
- Orders in progress
- Business impact metrics

#### **Saga Error Dashboard:**
- Error rates by type
- Error recovery duration
- Timeout statistics
- Compensation success rates

### **Key Metrics Visualization**

#### **Saga Flow Metrics:**
- **Gauge**: Active sagas by status
- **Line Chart**: Saga execution duration over time
- **Bar Chart**: Success/failure rates by saga type
- **Heatmap**: Step execution patterns

#### **Compensation Metrics:**
- **Counter**: Total compensations executed
- **Histogram**: Compensation duration distribution
- **Pie Chart**: Compensation reasons breakdown
- **Timeline**: Compensation event sequence

#### **Business Metrics:**
- **Gauge**: Orders in progress
- **Line Chart**: Order processing throughput
- **Bar Chart**: Order completion rates
- **Table**: Recent saga executions

## **7. Usage Examples**

### **Starting a Saga**

```csharp
// In a service
var orderId = Guid.NewGuid().ToString();
var customerId = "customer-123";
var totalAmount = 150.00m;
var items = new List<OrderItem> { /* order items */ };

await _sagaOrchestrator.StartOrderSagaAsync(orderId, customerId, totalAmount, items);
```

### **Handling Step Completion**

```csharp
// In a service that completes a step
var stepData = new { PaymentId = "payment-123", Amount = 150.00m };
await _sagaOrchestrator.HandleSagaStepCompletedAsync(sagaId, "ProcessPayment", 3, stepData);
```

### **Handling Step Failure**

```csharp
// In a service that fails a step
var errorMessage = "Payment processing failed - insufficient funds";
var stepData = new { PaymentAttempted = true, Error = errorMessage };
await _sagaOrchestrator.HandleSagaStepFailedAsync(sagaId, "ProcessPayment", 3, errorMessage, stepData);
```

### **Querying Saga State**

```csharp
// Get saga state
var sagaState = await _sagaOrchestrator.GetSagaStateAsync(sagaId);

// Get sagas by order
var sagaStates = await _sagaOrchestrator.GetSagaStatesByOrderIdAsync(orderId);
```

### **Testing Saga Scenarios**

```bash
# Start success scenario
curl -X POST http://localhost:5000/api/sagatest/run-complete-success-scenario

# Start failure scenario
curl -X POST http://localhost:5000/api/sagatest/run-complete-failure-scenario

# Get saga state
curl http://localhost:5000/api/sagatest/saga-state/{sagaId}
```

## **8. Benefits Achieved**

### **Data Consistency**
- **Eventual consistency** across services
- **Compensation mechanisms** for failures
- **Audit trail** of all operations

### **Reliability**
- **Failure isolation** - one service failure doesn't break others
- **Automatic compensation** on failures
- **Retry mechanisms** for transient failures

### **Scalability**
- **Decoupled services** can scale independently
- **Event-driven** coordination reduces coupling
- **Parallel processing** possible for independent steps

### **Observability**
- **Comprehensive metrics** for monitoring
- **Structured logging** for debugging
- **Real-time dashboards** for visualization

### **Maintainability**
- **Clear separation** of business logic and coordination
- **Easy to add** new steps or modify existing ones
- **Comprehensive testing** capabilities

## **9. Next Steps**

### **Enhancements**
1. **Saga Timeout Handling** - Add timeout mechanisms for long-running sagas
2. **Retry Policies** - Implement configurable retry strategies
3. **Saga Versioning** - Add support for saga schema versioning
4. **Dead Letter Queues** - Handle failed compensation scenarios
5. **Saga Monitoring Alerts** - Add alerting for critical saga failures

### **Production Readiness**
1. **Security** - Add authentication and authorization for saga operations
2. **Backup** - Implement saga state backup strategies
3. **Performance** - Optimize saga state queries and updates
4. **Testing** - Add comprehensive integration and load tests
5. **Documentation** - Add API documentation and operational runbooks

## **10. Conclusion**

The Saga pattern implementation provides a robust foundation for distributed transaction coordination in the CornerShop e-commerce platform. The choreographed approach ensures loose coupling between services while maintaining data consistency through compensation mechanisms.

The comprehensive observability features enable monitoring and debugging of complex distributed workflows, while the testing framework allows for validation of both success and failure scenarios.

This implementation successfully addresses the challenges of distributed transactions in microservices architecture, providing reliability, scalability, and maintainability for the order processing workflow. 