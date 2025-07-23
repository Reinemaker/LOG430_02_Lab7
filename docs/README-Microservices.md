# CornerShop Microservices Architecture

This project has been refactored from a monolithic architecture to a microservices architecture with the following services:

## Services Overview

### 1. Product Management Service (`ProductService`)
- **Purpose**: Manages product catalog and inventory
- **Database**: MongoDB
- **Cache**: Redis
- **Port**: 80 (internal)
- **API Endpoints**:
  - `GET /api/products` - Get all products
  - `GET /api/products/{id}` - Get product by ID
  - `GET /api/products/store/{storeId}` - Get products by store
  - `GET /api/products/category/{category}` - Get products by category
  - `GET /api/products/search?q={term}` - Search products
  - `GET /api/products/low-stock?threshold={number}` - Get low stock products
  - `POST /api/products` - Create new product
  - `PUT /api/products/{id}` - Update product
  - `PATCH /api/products/{id}/stock` - Update stock quantity
  - `DELETE /api/products/{id}` - Delete product

### 2. Customer Management Service (`CustomerService`)
- **Purpose**: Manages customer accounts and profiles
- **Database**: MongoDB
- **Cache**: Redis
- **Port**: 80 (internal)
- **API Endpoints**:
  - `GET /api/customers` - Get all customers
  - `GET /api/customers/{id}` - Get customer by ID
  - `GET /api/customers/email/{email}` - Get customer by email
  - `GET /api/customers/search?q={term}` - Search customers
  - `GET /api/customers/store/{storeId}` - Get customers by store
  - `POST /api/customers` - Create new customer
  - `PUT /api/customers/{id}` - Update customer
  - `PATCH /api/customers/{id}/deactivate` - Deactivate customer
  - `PATCH /api/customers/{id}/activate` - Activate customer
  - `PATCH /api/customers/{id}/stats` - Update customer statistics
  - `DELETE /api/customers/{id}` - Delete customer

### 3. Shopping Cart Service (`CartService`)
- **Purpose**: Manages shopping cart operations
- **Database**: Redis (for cart storage)
- **Cache**: Redis
- **Port**: 80 (internal)
- **API Endpoints**:
  - `GET /api/carts/{customerId}` - Get customer's cart
  - `POST /api/carts/{customerId}` - Create new cart
  - `POST /api/carts/{customerId}/items` - Add item to cart
  - `PUT /api/carts/{customerId}/items/{productId}` - Update cart item quantity
  - `DELETE /api/carts/{customerId}/items/{productId}` - Remove item from cart
  - `DELETE /api/carts/{customerId}/clear` - Clear cart
  - `DELETE /api/carts/{customerId}` - Delete cart

### 4. Order Management Service (`OrderService`)
- **Purpose**: Handles order validation and checkout process
- **Database**: MongoDB
- **Cache**: Redis
- **Port**: 80 (internal)
- **API Endpoints**:
  - `GET /api/orders` - Get all orders
  - `GET /api/orders/{id}` - Get order by ID
  - `GET /api/orders/number/{orderNumber}` - Get order by order number
  - `GET /api/orders/customer/{customerId}` - Get orders by customer
  - `GET /api/orders/store/{storeId}` - Get orders by store
  - `GET /api/orders/status/{status}` - Get orders by status
  - `GET /api/orders/date-range?startDate={date}&endDate={date}` - Get orders by date range
  - `POST /api/orders` - Create new order
  - `POST /api/orders/checkout` - Process checkout
  - `PATCH /api/orders/{id}/status` - Update order status
  - `PATCH /api/orders/{id}/payment-status` - Update payment status
  - `PATCH /api/orders/{id}/cancel` - Cancel order
  - `DELETE /api/orders/{id}` - Delete order

### 5. Sales Management Service (`SalesService`)
- **Purpose**: Manages sales and transactions (existing functionality)
- **Database**: MongoDB
- **Cache**: Redis
- **Port**: 80 (internal)

### 6. Reporting Service (`ReportingService`)
- **Purpose**: Handles reports and analytics (existing functionality)
- **Database**: MongoDB
- **Port**: 80 (internal)

### 7. Stock Service (`StockService`)
- **Purpose**: Shared stock management between physical store and online shop
- **Database**: MongoDB
- **Cache**: Redis
- **Port**: 80 (internal)

## Architecture Benefits

### Logical System Decomposition
- **Separation of Concerns**: Each service has a specific responsibility
- **Independent Deployment**: Services can be deployed independently
- **Technology Flexibility**: Each service can use different technologies if needed
- **Scalability**: Services can be scaled independently based on load

### Shared Services
- **Stock Service**: Shared between physical store and online shop
- **Shared Models**: Common data models across services
- **Shared Interfaces**: Common service contracts

### New APIs Added
1. **Customer Account Creation API**: Full CRUD operations for customer management
2. **Shopping Cart API**: Complete cart management with Redis storage
3. **Order Validation API**: Comprehensive order processing and checkout

## Infrastructure

### Docker Containers
Each service runs in its own Docker container:
- `product-service`
- `customer-service`
- `cart-service`
- `order-service`
- `sales-service`
- `reporting-service`
- `stock-service`

### Load Balancer
- **Traefik**: Reverse proxy and load balancer
- **Service Discovery**: Automatic service discovery via Docker labels

### Data Storage
- **MongoDB**: Primary database for most services
- **Redis**: Caching and cart storage
- **Persistent Volumes**: Data persistence across container restarts

### Monitoring
- **Prometheus**: Metrics collection
- **Grafana**: Monitoring dashboards
- **Health Checks**: Built-in health monitoring for each service

## Getting Started

### Prerequisites
- Docker and Docker Compose
- .NET 8.0 SDK (for development)

### Running the Microservices

1. **Start all services**:
   ```bash
   docker-compose -f docker-compose.microservices.yml up -d
   ```

2. **Access services**:
   - Product Service: `http://product.cornershop.localhost`
   - Customer Service: `http://customer.cornershop.localhost`
   - Cart Service: `http://cart.cornershop.localhost`
   - Order Service: `http://order.cornershop.localhost`
   - Traefik Dashboard: `http://traefik.localhost:8080`
   - Grafana: `http://localhost:3000` (admin/admin)

3. **API Documentation**:
   - Each service provides Swagger UI at `/swagger`
   - Example: `http://product.cornershop.localhost/swagger`

### Development

1. **Build shared library**:
   ```bash
   cd shared/CornerShop.Shared
   dotnet build
   ```

2. **Build individual services**:
   ```bash
   cd services/ProductService
   dotnet build
   ```

3. **Run services locally**:
   ```bash
   cd services/ProductService
   dotnet run
   ```

## Service Communication

### Inter-Service Communication
Services communicate via HTTP REST APIs. In a production environment, you might want to add:
- **API Gateway**: Centralized routing and authentication
- **Message Queue**: Asynchronous communication (RabbitMQ, Kafka)
- **Service Mesh**: Advanced service-to-service communication (Istio)

### Data Consistency
- **Eventual Consistency**: Services maintain their own data with eventual consistency
- **Saga Pattern**: For complex transactions spanning multiple services
- **CQRS**: Command Query Responsibility Segregation for read/write optimization

## Deployment Considerations

### Production Deployment
1. **Environment Variables**: Configure connection strings and secrets
2. **Health Checks**: Monitor service health
3. **Logging**: Centralized logging (ELK Stack)
4. **Security**: API authentication and authorization
5. **Backup**: Database backup strategies

### Scaling
- **Horizontal Scaling**: Scale services independently
- **Load Balancing**: Distribute traffic across service instances
- **Caching**: Redis caching for performance
- **Database Sharding**: For high-volume data

## Migration from Monolith

The original monolithic application (`CornerShop`) has been preserved. The microservices architecture runs alongside it, allowing for:
- **Gradual Migration**: Move functionality piece by piece
- **A/B Testing**: Compare monolith vs microservices performance
- **Rollback**: Easy rollback to monolith if needed

## Next Steps

1. **API Gateway**: Implement centralized routing and authentication
2. **Service Mesh**: Add Istio for advanced service communication
3. **Event-Driven Architecture**: Implement event sourcing and CQRS
4. **Automated Testing**: Add integration and contract tests
5. **CI/CD Pipeline**: Automated deployment pipeline
6. **Monitoring**: Enhanced observability and alerting 