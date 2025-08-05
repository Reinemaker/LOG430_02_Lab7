# CornerShop - Complete Technical Documentation

## Table of Contents
1. [Technical Overview](#technical-overview)
2. [Architecture Documentation](#architecture-documentation)
3. [Saga Implementation](#saga-implementation)
4. [Event-Driven Architecture](#event-driven-architecture)
5. [API Documentation](#api-documentation)
6. [Security and Access Management](#security-and-access-management)
7. [Load Testing and Performance](#load-testing-and-performance)
8. [Observability and Monitoring](#observability-and-monitoring)
9. [Controlled Failures and Resilience](#controlled-failures-and-resilience)
10. [Load Balancing Implementation](#load-balancing-implementation)
11. [CORS Configuration](#cors-configuration)
12. [Architecture Comparison](#architecture-comparison)
13. [Documentation and Testing](#documentation-and-testing)
14. [Domain-Driven Design](#domain-driven-design)

---

# Technical Overview

## Corner Shop - Technical Documentation

### Overview
Corner Shop is a distributed, web-based multi-store management system with a full REST API. Each store operates with its own local SQLite database for products and sales, supporting full offline operation. The head office uses a central MongoDB database for consolidated reporting and administration. A sync service allows admins to push all unsynced sales from local SQLite to MongoDB with a single click.

### Architecture
- **Web Client**: ASP.NET Core MVC
- **REST API**: ASP.NET Core Web API with full REST compliance
- **Store Data Layer**: Local SQLite per store
- **Central Data Layer**: MongoDB
- **Sync Service**: Handles pushing local sales to MongoDB

### Key Features
- Local, offline operation for each store
- Centralized, consolidated reporting for the head office
- One-click sync from the Reports page
- Modern, responsive web interface
- **Full REST API** with versioning, HATEOAS, and standardized error handling
- **Cross-Origin Support** for frontend integration
- **OpenAPI 3.0 Documentation** with Swagger UI and ReDoc

### Technology Stack

#### Core Technologies
- **.NET 8.0**: Chosen for its robust performance, cross-platform capabilities, and modern C# features
- **ASP.NET Core MVC**: Web framework for the user interface
- **ASP.NET Core Web API**: REST API framework with full compliance
- **MongoDB**: Selected for its flexibility with document-based storage and ease of scaling
- **SQLite**: Local database for each store's offline operations
- **Docker**: Used for containerization to ensure consistent deployment across environments

#### Development Tools
- **xUnit**: For unit testing
- **Moq**: For mocking dependencies in tests
- **dotnet-format**: For code formatting and style consistency
- **Swagger/OpenAPI**: For API documentation and testing

---

# Architecture Documentation

## Architecture Overview

The CornerShop system is built using a microservices architecture with the following key components:

### Core Services
1. **ProductService**: Manages product catalog and inventory
2. **CustomerService**: Handles customer information and profiles
3. **CartService**: Manages shopping cart operations
4. **OrderService**: Processes orders and manages order lifecycle
5. **PaymentService**: Handles payment processing
6. **StockService**: Manages inventory and stock levels
7. **SalesService**: Tracks sales and revenue
8. **ReportingService**: Generates reports and analytics

### Infrastructure Services
1. **ApiGateway**: Single entry point for all client requests
2. **SagaOrchestrator**: Manages distributed transactions
3. **ChoreographedSagaCoordinator**: Coordinates event-driven sagas
4. **EventPublisher**: Publishes business events
5. **EventStore**: Stores and manages events
6. **NotificationService**: Handles notifications

### Data Storage
- **MongoDB**: Primary database for most services
- **Redis**: Caching and session management
- **Event Store**: For event sourcing and CQRS

### Communication Patterns
- **Synchronous**: REST APIs for direct service communication
- **Asynchronous**: Event-driven communication via message queues
- **Saga Pattern**: For distributed transaction management

## Microservices Architecture

### Service Boundaries
Each service is designed around a specific business domain:

#### Product Domain
- **ProductService**: Product catalog, categories, pricing
- **StockService**: Inventory management, stock levels, reservations

#### Customer Domain
- **CustomerService**: Customer profiles, preferences, history

#### Order Domain
- **CartService**: Shopping cart management
- **OrderService**: Order processing and lifecycle
- **PaymentService**: Payment processing and validation

#### Analytics Domain
- **SalesService**: Sales tracking and analytics
- **ReportingService**: Report generation and data aggregation

### Service Communication
Services communicate through:
1. **REST APIs**: For synchronous requests
2. **Event Messages**: For asynchronous communication
3. **Shared Database**: For data consistency (when appropriate)

---

# Saga Implementation

## Saga Pattern Overview

The CornerShop system implements the Saga pattern to manage distributed transactions across multiple microservices. This ensures data consistency in a distributed environment where traditional ACID transactions are not possible.

### Saga Types Implemented

#### 1. Orchestrated Saga
- **Coordinator**: SagaOrchestrator service
- **Participants**: OrderService, StockService, PaymentService
- **Flow**: Sequential execution with compensation on failure

#### 2. Choreographed Saga
- **Coordinator**: ChoreographedSagaCoordinator service
- **Participants**: All saga-capable services
- **Flow**: Event-driven with local decision making

## Saga State Management

### State Transitions
```csharp
public enum SagaStatus
{
    Started,
    InProgress,
    StockVerifying,
    StockVerified,
    StockReserving,
    StockReserved,
    PaymentProcessing,
    PaymentProcessed,
    OrderConfirming,
    Completed,
    Failed,
    Compensating,
    Compensated,
    Aborted
}
```

### Saga Steps
```csharp
public static class SagaSteps
{
    public const string CreateOrder = "CreateOrder";
    public const string ReserveStock = "ReserveStock";
    public const string ProcessPayment = "ProcessPayment";
    public const string ConfirmOrder = "ConfirmOrder";
    public const string SendNotifications = "SendNotifications";
}
```

## Saga Implementation Details

### Orchestrated Saga Flow
1. **Create Order**: OrderService creates the order
2. **Reserve Stock**: StockService reserves inventory
3. **Process Payment**: PaymentService processes payment
4. **Confirm Order**: OrderService confirms the order
5. **Send Notifications**: NotificationService sends confirmations

### Compensation Actions
- **Order Cancellation**: If payment fails, cancel the order
- **Stock Release**: If order fails, release reserved stock
- **Payment Refund**: If order is cancelled, refund payment

### Saga Metrics and Monitoring
- **Success Rate**: Percentage of successful saga completions
- **Average Duration**: Time taken for saga completion
- **Failure Rate**: Percentage of failed sagas
- **Compensation Rate**: Percentage of sagas requiring compensation

---

# Event-Driven Architecture

## Event-Driven Design Principles

The CornerShop system uses event-driven architecture to enable loose coupling between services and support asynchronous processing.

### Event Types

#### Business Events
```csharp
public enum BusinessEventType
{
    OrderCreated,
    OrderConfirmed,
    OrderCancelled,
    PaymentProcessed,
    PaymentFailed,
    StockReserved,
    StockReleased,
    ProductUpdated,
    CustomerRegistered
}
```

#### Event Structure
```csharp
public class BusinessEvent
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; }
    public object Data { get; set; }
    public string Source { get; set; }
}
```

### Event Publishing
Services publish events using the EventPublisher service:
```csharp
public interface IEventProducer
{
    Task PublishEventAsync(string eventType, object data, string correlationId);
    Task PublishEventAsync(BusinessEvent businessEvent);
}
```

### Event Consumption
Services consume events using the EventConsumer:
```csharp
public interface IEventConsumer
{
    Task ConsumeEventAsync(BusinessEvent businessEvent);
    Task SubscribeToEventAsync(string eventType, Func<BusinessEvent, Task> handler);
}
```

## Event Store Implementation

### Event Persistence
Events are stored in MongoDB for:
- **Audit Trail**: Complete history of all business events
- **Event Sourcing**: Rebuilding service state from events
- **Analytics**: Business intelligence and reporting

### Event Replay
The system supports event replay for:
- **Service Recovery**: Rebuilding service state after failures
- **Testing**: Replaying events for testing scenarios
- **Analytics**: Historical analysis and reporting

---

# API Documentation

## REST API Design

### API Principles
The CornerShop API follows REST principles:
- **Stateless**: Each request contains all necessary information
- **Cacheable**: Responses include appropriate cache headers
- **Uniform Interface**: Standard HTTP methods and URI patterns
- **Layered System**: API abstracts underlying complexity
- **HATEOAS**: Hypermedia links in all responses

### API Features
- **Versioning**: All endpoints use `/api/v1/` prefix
- **HATEOAS**: Navigation links in all responses
- **Standardized Errors**: Consistent error format with timestamp, status, and path
- **HTTP Status Codes**: Proper use of 200, 201, 204, 400, 404, 500
- **Caching**: Response caching with appropriate durations
- **Content Negotiation**: Support for JSON and XML formats
- **PATCH Support**: Partial updates for all resources

### API Endpoints

#### Product Endpoints
```
GET    /api/v1/products              # Get all products
GET    /api/v1/products/{id}         # Get product by ID
POST   /api/v1/products              # Create new product
PUT    /api/v1/products/{id}         # Update product
PATCH  /api/v1/products/{id}         # Partial update
DELETE /api/v1/products/{id}         # Delete product
GET    /api/v1/products/search       # Search products
```

#### Order Endpoints
```
GET    /api/v1/orders                # Get all orders
GET    /api/v1/orders/{id}           # Get order by ID
POST   /api/v1/orders                # Create new order
PUT    /api/v1/orders/{id}           # Update order
DELETE /api/v1/orders/{id}           # Cancel order
GET    /api/v1/orders/customer/{id}  # Get customer orders
```

#### Customer Endpoints
```
GET    /api/v1/customers             # Get all customers
GET    /api/v1/customers/{id}        # Get customer by ID
POST   /api/v1/customers             # Create new customer
PUT    /api/v1/customers/{id}        # Update customer
DELETE /api/v1/customers/{id}        # Delete customer
```

### API Documentation
- **Swagger UI**: Interactive API documentation
- **ReDoc**: Alternative API documentation
- **OpenAPI Specification**: Machine-readable API specification

---

# Security and Access Management

## Security Implementation

### API Security
- **API Key Authentication**: Required for all API endpoints
- **Rate Limiting**: Prevents abuse and ensures fair usage
- **Input Validation**: Comprehensive validation of all inputs
- **SQL Injection Prevention**: Parameterized queries and input sanitization

### Access Control
- **Role-Based Access Control (RBAC)**: Different permissions for different roles
- **Resource-Level Permissions**: Fine-grained access control
- **Audit Logging**: Complete audit trail of all actions

### Data Protection
- **Encryption at Rest**: Sensitive data encrypted in databases
- **Encryption in Transit**: TLS/SSL for all communications
- **Data Masking**: Sensitive data masked in logs and responses

## Security Headers
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

---

# Load Testing and Performance

## Load Testing Strategy

### Test Scenarios
1. **Baseline Performance**: Measure system performance under normal load
2. **Peak Load Testing**: Test system behavior under high load
3. **Stress Testing**: Determine system breaking point
4. **Endurance Testing**: Test system stability over time
5. **Spike Testing**: Test system response to sudden load increases

### Performance Metrics
- **Response Time**: Average, 95th percentile, 99th percentile
- **Throughput**: Requests per second
- **Error Rate**: Percentage of failed requests
- **Resource Utilization**: CPU, memory, network usage

### Load Testing Tools
- **Artillery**: For API load testing
- **JMeter**: For comprehensive load testing
- **Custom Scripts**: For specific business scenarios

## Performance Optimization

### Caching Strategy
- **Redis Caching**: For frequently accessed data
- **Response Caching**: For API responses
- **Database Query Optimization**: Optimized queries and indexes

### Database Optimization
- **Indexing**: Proper indexes on frequently queried fields
- **Connection Pooling**: Efficient database connection management
- **Query Optimization**: Optimized SQL queries

---

# Observability and Monitoring

## Monitoring Stack

### Metrics Collection
- **Prometheus**: Time-series metrics collection
- **Grafana**: Metrics visualization and dashboards
- **Custom Metrics**: Business-specific metrics

### Logging
- **Structured Logging**: JSON-formatted logs
- **Log Levels**: Debug, Info, Warning, Error
- **Log Aggregation**: Centralized log collection

### Tracing
- **Distributed Tracing**: Track requests across services
- **Performance Monitoring**: Identify bottlenecks
- **Error Tracking**: Monitor and alert on errors

## Health Checks
```csharp
public class HealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Check service health
        return HealthCheckResult.Healthy();
    }
}
```

---

# Controlled Failures and Resilience

## Failure Scenarios

### Service Failures
- **Network Failures**: Simulate network connectivity issues
- **Database Failures**: Simulate database unavailability
- **Service Crashes**: Simulate service crashes and restarts

### Resilience Patterns
- **Circuit Breaker**: Prevent cascading failures
- **Retry Logic**: Automatic retry with exponential backoff
- **Fallback Mechanisms**: Alternative paths when services fail
- **Graceful Degradation**: Continue operation with reduced functionality

### Chaos Engineering
- **Random Failures**: Inject random failures to test resilience
- **Load Testing**: Test system behavior under stress
- **Recovery Testing**: Test system recovery after failures

---

# Load Balancing Implementation

## Load Balancing Strategy

### Load Balancer Configuration
- **Round Robin**: Distribute requests evenly across instances
- **Least Connections**: Route to instance with fewest connections
- **Health Checks**: Remove unhealthy instances from rotation

### Service Discovery
- **Service Registry**: Dynamic service registration and discovery
- **Health Monitoring**: Continuous health monitoring of services
- **Load Distribution**: Intelligent load distribution

### Scaling
- **Horizontal Scaling**: Add more service instances
- **Auto Scaling**: Automatic scaling based on load
- **Resource Monitoring**: Monitor resource usage for scaling decisions

---

# CORS Configuration

## Cross-Origin Resource Sharing

### CORS Policies
```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### Security Considerations
- **Origin Validation**: Validate allowed origins
- **Method Restrictions**: Restrict HTTP methods as needed
- **Header Restrictions**: Restrict allowed headers
- **Credentials**: Handle credentials appropriately

---

# Architecture Comparison

## Monolithic vs Microservices

### Monolithic Architecture
**Advantages:**
- Simpler development and deployment
- Easier testing and debugging
- Better performance for small applications

**Disadvantages:**
- Difficult to scale individual components
- Technology lock-in
- Deployment complexity for large applications

### Microservices Architecture
**Advantages:**
- Independent scaling of services
- Technology diversity
- Easier maintenance and updates
- Better fault isolation

**Disadvantages:**
- Increased complexity
- Network overhead
- Distributed system challenges
- Data consistency issues

## Performance Comparison

### Response Times
- **Monolithic**: Lower latency due to in-process calls
- **Microservices**: Higher latency due to network calls

### Throughput
- **Monolithic**: Limited by single application capacity
- **Microservices**: Can scale individual services

### Resource Utilization
- **Monolithic**: Higher memory usage per instance
- **Microservices**: Lower memory usage per service

---

# Documentation and Testing

## Documentation Standards

### Code Documentation
- **XML Comments**: Comprehensive code documentation
- **README Files**: Project and service documentation
- **API Documentation**: OpenAPI/Swagger documentation

### Architecture Documentation
- **Architecture Decision Records (ADR)**: Document architectural decisions
- **System Diagrams**: Visual representation of system architecture
- **Data Models**: Database and data structure documentation

## Testing Strategy

### Unit Testing
- **Test Coverage**: Aim for 80%+ code coverage
- **Mocking**: Use mocks for external dependencies
- **Test Data**: Use test data factories

### Integration Testing
- **Service Integration**: Test service interactions
- **Database Integration**: Test database operations
- **API Testing**: Test API endpoints

### End-to-End Testing
- **User Scenarios**: Test complete user workflows
- **System Integration**: Test entire system integration
- **Performance Testing**: Test system performance

---

# Domain-Driven Design

## DDD Implementation

### Bounded Contexts
1. **Product Context**: Product catalog and inventory management
2. **Customer Context**: Customer management and profiles
3. **Order Context**: Order processing and management
4. **Payment Context**: Payment processing and validation
5. **Analytics Context**: Reporting and analytics

### Domain Models
- **Entities**: Objects with identity and lifecycle
- **Value Objects**: Immutable objects without identity
- **Aggregates**: Clusters of related entities
- **Services**: Domain logic that doesn't belong to entities

### Ubiquitous Language
- **Domain Terms**: Consistent terminology across the system
- **Business Rules**: Clear expression of business rules
- **User Stories**: Written in domain language

---

# Conclusion

This comprehensive technical documentation provides a complete overview of the CornerShop system architecture, implementation details, and technical considerations. The system demonstrates modern microservices patterns, event-driven architecture, and robust distributed transaction management through the Saga pattern.

The documentation serves as a reference for developers, architects, and operations teams working with the CornerShop system, providing detailed information about design decisions, implementation patterns, and operational considerations. 