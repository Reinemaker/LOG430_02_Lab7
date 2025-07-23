# Controlled Failures in Saga Orchestration

## Overview

The CornerShop system includes a comprehensive controlled failure simulation system that allows testing and observing how failures affect saga orchestration and state machine transitions. This implementation satisfies the requirement to introduce controlled failures and observe their effects on the saga and state machine.

## Implementation Details

### 1. Controlled Failure Service

The `IControlledFailureService` and `ControlledFailureService` provide configurable failure simulation:

```csharp
public interface IControlledFailureService
{
    Task<bool> SimulateInsufficientStockAsync(string productName, string storeId, int requestedQuantity, string? sagaId = null);
    Task<bool> SimulatePaymentFailureAsync(decimal amount, string customerId, string? sagaId = null);
    Task<bool> SimulateNetworkTimeoutAsync(string serviceName, string? sagaId = null);
    Task<bool> SimulateDatabaseFailureAsync(string operation, string? sagaId = null);
    Task<bool> SimulateServiceUnavailableAsync(string serviceName, string? sagaId = null);
    Dictionary<string, object> GetFailureConfiguration();
    void UpdateFailureConfiguration(Dictionary<string, object> config);
}
```

### 2. Failure Types Implemented

#### Insufficient Stock Failure
- **Trigger**: During stock validation in ProductService
- **Effect**: Simulates stock unavailability for products
- **Compensation**: Stock reservation is released
- **Configuration**: Configurable probability, critical products/stores

#### Payment Failure
- **Trigger**: During sale total calculation in SaleService
- **Effect**: Simulates payment processing failures
- **Compensation**: Sale is cancelled, stock is restored
- **Configuration**: Higher probability for large amounts

#### Network Timeout
- **Trigger**: During service calls
- **Effect**: Simulates network connectivity issues
- **Compensation**: Service-specific rollback actions
- **Configuration**: Configurable probability per service

#### Database Failure
- **Trigger**: During database operations
- **Effect**: Simulates database connection problems
- **Compensation**: Transaction rollback
- **Configuration**: Configurable probability per operation

#### Service Unavailable
- **Trigger**: During service calls
- **Effect**: Simulates service unavailability
- **Compensation**: Service-specific rollback actions
- **Configuration**: Configurable probability per service

### 3. Enhanced Compensation System

The system includes detailed compensation tracking with the `CompensationResult` model:

```csharp
public class CompensationResult
{
    public string StepId { get; set; }
    public string ServiceName { get; set; }
    public string Action { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt);
}
```

### 4. State Machine Integration

Failures are integrated with the saga state machine:

- **State Transitions**: Failures trigger state transitions to `Failed` or `Compensating`
- **Event Publishing**: All failures publish events with detailed information
- **Compensation Tracking**: Compensation results are stored and tracked
- **Real-time Monitoring**: State changes are observable in real-time

## Configuration

### Default Configuration

```json
{
  "EnableFailures": true,
  "InsufficientStockProbability": 0.1,
  "PaymentFailureProbability": 0.05,
  "NetworkTimeoutProbability": 0.03,
  "DatabaseFailureProbability": 0.02,
  "ServiceUnavailableProbability": 0.01,
  "FailureDelayMs": 1000,
  "CriticalProducts": ["Premium Coffee", "Organic Milk"],
  "CriticalStores": ["store_001", "store_002"]
}
```

### Configuration Options

- **EnableFailures**: Master switch to enable/disable all failures
- **InsufficientStockProbability**: Probability of stock insufficiency (0.0-1.0)
- **PaymentFailureProbability**: Probability of payment failure (0.0-1.0)
- **NetworkTimeoutProbability**: Probability of network timeout (0.0-1.0)
- **DatabaseFailureProbability**: Probability of database failure (0.0-1.0)
- **ServiceUnavailableProbability**: Probability of service unavailability (0.0-1.0)
- **FailureDelayMs**: Artificial delay before failure (milliseconds)
- **CriticalProducts**: List of products with higher failure probability
- **CriticalStores**: List of stores with higher failure probability

## API Endpoints

### Failure Configuration

#### GET /api/ControlledFailure/config
Get current failure configuration.

**Response:**
```json
{
  "success": true,
  "configuration": {
    "EnableFailures": true,
    "InsufficientStockProbability": 0.1,
    "PaymentFailureProbability": 0.05,
    "NetworkTimeoutProbability": 0.03,
    "DatabaseFailureProbability": 0.02,
    "ServiceUnavailableProbability": 0.01,
    "FailureDelayMs": 1000,
    "CriticalProducts": ["Premium Coffee", "Organic Milk"],
    "CriticalStores": ["store_001", "store_002"]
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### PUT /api/ControlledFailure/config
Update failure configuration.

**Request Body:**
```json
{
  "EnableFailures": true,
  "InsufficientStockProbability": 0.3,
  "PaymentFailureProbability": 0.2,
  "NetworkTimeoutProbability": 0.15,
  "DatabaseFailureProbability": 0.1,
  "ServiceUnavailableProbability": 0.05,
  "FailureDelayMs": 500
}
```

#### POST /api/ControlledFailure/toggle
Enable or disable controlled failures.

**Request Body:**
```json
true
```

### Failure Simulation

#### POST /api/ControlledFailure/simulate
Simulate a specific failure type.

**Request Body:**
```json
{
  "failureType": "insufficientstock",
  "productName": "Premium Coffee",
  "storeId": "store_001",
  "quantity": 100
}
```

**Response:**
```json
{
  "success": true,
  "failureTriggered": true,
  "result": "Controlled failure: Insufficient stock for Premium Coffee in store store_001. Requested: 100",
  "failureType": "insufficientstock",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Monitoring and Statistics

#### GET /api/ControlledFailure/affected-sagas
Get sagas affected by failures.

**Response:**
```json
{
  "success": true,
  "affectedSagas": [
    {
      "sagaId": "saga_123",
      "sagaType": "SaleSaga",
      "currentState": "Failed",
      "errorMessage": "Controlled failure: Insufficient stock for Premium Coffee",
      "createdAt": "2024-01-15T10:30:00Z",
      "completedAt": "2024-01-15T10:30:05Z",
      "transitionCount": 3,
      "lastTransition": {
        "id": "transition_456",
        "fromState": "StockReserved",
        "toState": "Failed",
        "serviceName": "ProductService",
        "action": "ValidateStockAvailability",
        "eventType": "Failure",
        "message": "Controlled failure: Insufficient stock for Premium Coffee",
        "timestamp": "2024-01-15T10:30:05Z"
      }
    }
  ],
  "totalAffected": 1,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/ControlledFailure/compensation-stats
Get compensation statistics.

**Response:**
```json
{
  "success": true,
  "statistics": {
    "totalSagas": 10,
    "compensatedSagas": 3,
    "failedSagas": 2,
    "compensationRate": 0.3,
    "failureRate": 0.2,
    "recentFailures": 1,
    "recentCompensations": 2
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Testing and Observation

### Running the Test Script

```bash
./test-controlled-failures.sh
```

The test script demonstrates:

1. **Configuration Management**: Setting different failure probabilities
2. **Failure Simulation**: Testing individual failure types
3. **Saga Execution**: Running sagas with failures enabled
4. **State Monitoring**: Observing state machine transitions
5. **Compensation Tracking**: Monitoring compensation actions
6. **Statistics Collection**: Gathering failure and compensation statistics

### Observing Effects

When failures occur, you can observe:

#### 1. State Machine Transitions
- Saga state changes from `Started` → `StoreValidated` → `StockReserved` → `Failed`
- Compensation state: `Compensating` → `Compensated`
- Real-time state updates via API endpoints

#### 2. Event Publishing
- Each failure publishes events with detailed information
- Events include failure type, context, and error messages
- Events are stored and queryable via API

#### 3. Compensation Actions
- Automatic rollback of completed steps
- Detailed compensation results with success/failure tracking
- Compensation duration and error information

#### 4. Statistics and Monitoring
- Failure rates by type
- Compensation success rates
- Affected saga analysis
- Real-time monitoring capabilities

## Integration with Services

### ProductService Integration

```csharp
public async Task<bool> ValidateStockAvailability(string productName, string storeId, int quantity, string? sagaId = null)
{
    try
    {
        // Simulate controlled failure for stock validation
        await _failureService.SimulateInsufficientStockAsync(productName, storeId, quantity, sagaId);
        
        // Normal validation logic...
    }
    catch (Exception ex)
    {
        // Event publishing and error handling...
    }
}
```

### SaleService Integration

```csharp
public async Task<decimal> CalculateSaleTotal(List<SaleItem> items, string storeId, string? sagaId = null)
{
    try
    {
        // Simulate controlled failures
        await _failureService.SimulateNetworkTimeoutAsync("SaleService", sagaId);
        await _failureService.SimulateServiceUnavailableAsync("SaleService", sagaId);
        
        // Calculate total...
        
        // Simulate payment failure for high amounts
        await _failureService.SimulatePaymentFailureAsync(total, "customer_001", sagaId);
        
        // Return total...
    }
    catch (Exception ex)
    {
        // Event publishing and error handling...
    }
}
```

## Benefits

### 1. Testing and Validation
- **Realistic Testing**: Simulate real-world failure scenarios
- **Edge Case Coverage**: Test system behavior under various failure conditions
- **Regression Testing**: Ensure compensation mechanisms work correctly

### 2. System Resilience
- **Fault Tolerance**: Verify system handles failures gracefully
- **Compensation Validation**: Ensure rollback actions work correctly
- **State Consistency**: Verify state machine maintains consistency

### 3. Observability
- **Failure Analysis**: Understand how failures propagate through the system
- **Performance Impact**: Measure the impact of failures on system performance
- **Compensation Efficiency**: Monitor compensation success rates and timing

### 4. Development and Debugging
- **Controlled Environment**: Test failures in a controlled manner
- **Reproducible Scenarios**: Create reproducible failure scenarios
- **Debugging Support**: Detailed logging and event tracking for debugging

## Best Practices

### 1. Configuration Management
- Start with low failure probabilities in production
- Use higher probabilities for testing and development
- Monitor and adjust probabilities based on system behavior

### 2. Testing Strategy
- Test individual failure types in isolation
- Test combinations of failures
- Test compensation mechanisms thoroughly
- Monitor system performance under failure conditions

### 3. Monitoring and Alerting
- Set up alerts for high failure rates
- Monitor compensation success rates
- Track affected saga statistics
- Set up dashboards for failure analysis

### 4. Documentation
- Document failure scenarios and their effects
- Maintain runbooks for common failure patterns
- Document compensation mechanisms and their triggers

## Conclusion

The controlled failures implementation provides a comprehensive testing and observation framework for saga orchestration. It allows developers and operators to:

- Test system resilience under various failure conditions
- Observe how failures affect the state machine and compensation mechanisms
- Validate that rollback actions work correctly
- Monitor system behavior in realistic failure scenarios

This implementation satisfies the requirement to introduce controlled failures and observe their effects on the saga and state machine, providing valuable insights into system behavior and resilience. 