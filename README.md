# Corner Shop Multi-Store Management System

A modern, distributed point-of-sale and inventory management system for multi-store businesses. Each store operates independently with a local SQLite database, while the head office consolidates data using a central MongoDB database. The system is designed for reliability, offline support, and easy synchronization.

## 🚀 Quick Start

### Prerequisites
- Docker and Docker Compose
- k6 (for load testing)

### One-Command Setup
```bash
./quick-start.sh
```

This script will:
- Install Docker and Docker Compose (if not present)
- Install k6 (if not present)
- Start all services with proper configuration
- Wait for services to be ready

## 🌐 Access URLs

### Main Application
- **Primary URL**: `http://cornershop.localhost`
- **Alternative**: `http://localhost` (with proper hostname configuration)

### Monitoring & Management
- **Traefik Dashboard**: `http://traefik.localhost:8080`
- **Grafana Monitoring**: `http://localhost:3000` (admin/admin)
- **Prometheus Metrics**: `http://localhost:9090`

### API Documentation
- **Swagger UI**: `http://cornershop.localhost/api-docs`
- **ReDoc UI**: `http://cornershop.localhost/redoc`
- **API Documentation**: `http://cornershop.localhost/Home/ApiDocumentation`

## 🔧 Port Configuration

| Service | Port | Description |
|---------|------|-------------|
| **Traefik** | 80, 443, 8080 | Load balancer and reverse proxy |
| **CornerShop Apps** | 5000 (internal) | Main application instances |
| **MongoDB** | 27017 | Database server |
| **Redis** | 6379 | Caching and session storage |
| **Grafana** | 3000 | Monitoring dashboard |
| **Prometheus** | 9090 | Metrics collection |

## 🆕 New Features & Architecture

### Load Balancing & High Availability
- **Multiple App Instances**: 3 identical CornerShop application instances for load balancing
- **Traefik Integration**: Advanced reverse proxy with automatic service discovery
- **Sticky Sessions**: Session persistence across load balancer requests
- **Health Checks**: Automatic health monitoring and failover

### Containerized Architecture
- **Docker Compose**: Complete containerized deployment
- **Microservices Ready**: Infrastructure supports microservices architecture
- **Persistent Storage**: MongoDB and Redis data persistence
- **Environment Isolation**: Production-ready environment configuration

### Saga Orchestration
- **Distributed Transaction Management**: Coordinated transactions across microservices
- **Automatic Compensation**: Rollback mechanisms for failed transactions
- **Fault Tolerance**: Graceful handling of partial failures
- **Observability**: Detailed logging and monitoring of transaction flows
- **Event Publishing**: Microservices publish success/failure events after processing
- **State Machine**: Explicit state machine with enum, logging, and persistence
- **State Tracking**: Real-time state updates and transition history
- **Controlled Failures**: Configurable failure simulation for testing and observation
- **Enhanced Compensation**: Detailed compensation tracking and results

### Choreographed Saga Pattern
- **Event-Driven Coordination**: Services communicate through events for loose coupling
- **Automatic Saga Tracking**: ChoreographedSagaCoordinator tracks saga state and progress
- **Compensation Events**: Automatic compensation triggered by failure events
- **Real-time Monitoring**: Comprehensive metrics and statistics for saga performance
- **Redis Stream Integration**: Event streaming using Redis for reliable message delivery
- **Prometheus Metrics**: Detailed saga metrics for observability and alerting
- **Grafana Dashboards**: Visual monitoring of saga performance and health
- **API Endpoints**: RESTful APIs for saga state management and monitoring

### Monitoring & Observability
- **Prometheus**: Metrics collection and storage
- **Grafana**: Real-time monitoring dashboards with saga state evolution visualization
- **Health Endpoints**: `/health` and `/health/ready` endpoints
- **Application Metrics**: Built-in Prometheus metrics
- **Saga Metrics**: Comprehensive saga orchestration metrics (duration, failures, steps, state transitions)
- **Structured Logging**: Business events and decisions with JSON format
- **Real-time Monitoring**: Live saga state tracking and performance analysis

### Database & Caching
- **MongoDB Authentication**: Secure database access with credentials
- **Redis Caching**: Session storage and application caching
- **Connection Pooling**: Optimized database connections
- **Data Persistence**: Volume-based data storage

## 🏗️ Architecture

### Container Services
- **CornerShop Apps** (3 instances): Main web application with load balancing
- **Traefik**: Reverse proxy and load balancer
- **MongoDB**: Central database with authentication
- **Redis**: Caching and session storage
- **Prometheus**: Metrics collection
- **Grafana**: Monitoring visualization

### Network Architecture
- **Docker Network**: Isolated container communication
- **Port Mapping**: Strategic port exposure for external access
- **Service Discovery**: Automatic service registration with Traefik
- **SSL/TLS Ready**: HTTPS configuration support

## 📋 Features
- **Multi-store support**: Each store has its own local SQLite database for products and sales.
- **Offline operation**: Stores can operate independently, even without internet connectivity.
- **Centralized reporting**: The head office uses MongoDB to view consolidated sales and stock across all stores.
- **One-click sync**: Admins can synchronize all stores' local data to the central database from the Reports page.
- **Modern web interface**: Built with ASP.NET Core MVC for a responsive, user-friendly experience.
- **REST API**: Full REST-compliant API with versioning, HATEOAS, and standardized error handling.
- **Cross-Origin Support**: CORS-enabled for frontend integration.
- **Load Balancing**: Multiple application instances for high availability.
- **Monitoring**: Real-time metrics and health monitoring.
- **Containerized**: Complete Docker-based deployment.

## 🔐 Database Access

### MongoDB
- **Host**: `localhost:27017`
- **Username**: `admin`
- **Password**: `password`
- **Database**: `cornerShop`
- **Connection String**: `mongodb://admin:password@localhost:27017`

### Redis
- **Host**: `localhost:6379`
- **No Authentication**: Development configuration

## 🛠️ Management Commands

### Start Services
```bash
docker-compose up -d
```

### Stop Services
```bash
docker-compose down
```

### View Logs
```bash
# All services
docker-compose logs

# Specific service
docker-compose logs cornershop-app-1
```

### Rebuild and Restart
```bash
docker-compose up -d --build
```

### Check Service Status
```bash
docker ps
```

### Test Saga Orchestration
```bash
./test-saga-orchestration.sh
```

### Test Saga State Machine
```bash
./test-saga-state-machine.sh
```

### Test Controlled Failures
```bash
./test-controlled-failures.sh
```

### Test Saga Metrics & Monitoring
```bash
./test-saga-metrics-monitoring.sh
```

### Test Choreographed Saga Pattern
```bash
./test-choreographed-saga.sh
```

## 🔍 Troubleshooting

### Application Not Accessible
1. Ensure services are running: `docker ps`
2. Check Traefik logs: `docker-compose logs traefik`
3. Verify hostname resolution: Add `127.0.0.1 cornershop.localhost` to `/etc/hosts`

### Database Connection Issues
1. Verify MongoDB is running: `docker-compose logs mongodb`
2. Check connection string in environment variables
3. Ensure authentication credentials are correct

### Port Conflicts
- If ports are already in use, stop conflicting services
- Check port usage: `netstat -tlnp | grep :<port>`

## REST API Features
- **API Versioning**: All endpoints use `/api/v1/` prefix for future compatibility
- **HATEOAS**: Hypermedia links in all responses for navigation
- **Standardized Error Responses**: Consistent error format with timestamp, status, and path
- **HTTP Status Codes**: Proper use of 200, 201, 204, 400, 404, 500
- **Caching Headers**: Response caching with appropriate durations
- **Content Negotiation**: Support for JSON and XML formats via Accept header
- **PATCH Support**: Partial updates for all resources
- **OpenAPI 3.0 Documentation**: Complete Swagger/OpenAPI documentation
- **Saga Orchestration**: Distributed transaction management with compensation support

## API Endpoints
### Products API (`/api/v1/products`)
- `GET /api/v1/products` - Get all products
- `GET /api/v1/products/{id}` - Get specific product
- `GET /api/v1/products/store/{storeId}` - Get products by store
- `GET /api/v1/products/search` - Search products
- `GET /api/v1/products/low-stock` - Get low stock products
- `POST /api/v1/products` - Create new product
- `PUT /api/v1/products/{id}` - Update product (full update)
- `PATCH /api/v1/products/{id}` - Partially update product
- `DELETE /api/v1/products/{id}` - Delete product

### Stores API (`/api/v1/stores`)
- `GET /api/v1/stores` - Get all stores
- `GET /api/v1/stores/{id}` - Get specific store
- `GET /api/v1/stores/search` - Search stores
- `POST /api/v1/stores` - Create new store
- `PUT /api/v1/stores/{id}` - Update store (full update)
- `PATCH /api/v1/stores/{id}` - Partially update store
- `DELETE /api/v1/stores/{id}` - Delete store

### Sales API (`/api/v1/sales`)
- `GET /api/v1/sales/store/{storeId}/recent` - Get recent sales for store
- `GET /api/v1/sales/{id}` - Get specific sale
- `GET /api/v1/sales/{id}/details` - Get sale details with items
- `GET /api/v1/sales/date-range` - Get sales by date range
- `POST /api/v1/sales` - Create new sale
- `POST /api/v1/sales/{id}/cancel` - Cancel sale
- `PATCH /api/v1/sales/{id}` - Partially update sale

### Reports API (`/api/v1/reports`)
- `GET /api/v1/reports/sales/consolidated` - Get consolidated sales report
- `GET /api/v1/reports/inventory` - Get inventory report
- `GET /api/v1/reports/products/top-selling` - Get top selling products
- `GET /api/v1/reports/sales/trend` - Get sales trend report

### Saga Orchestration API (`/api/v1/saga`)
- `POST /api/v1/saga/sale` - Execute sale saga with distributed transaction management
- `POST /api/v1/saga/order` - Execute order saga with distributed transaction management
- `POST /api/v1/saga/stock` - Execute stock update saga with distributed transaction management
- `POST /api/v1/saga/compensate/{sagaId}` - Compensate a failed saga by rolling back completed steps

### Choreographed Saga API (`/api/choreographedsaga`)
- `GET /api/choreographedsaga/states` - Get all saga states
- `GET /api/choreographedsaga/state/{sagaId}` - Get specific saga state
- `GET /api/choreographedsaga/states/status/{status}` - Get sagas by status
- `GET /api/choreographedsaga/states/business-process/{businessProcess}` - Get sagas by business process
- `GET /api/choreographedsaga/states/date-range` - Get sagas by date range
- `GET /api/choreographedsaga/statistics` - Get saga statistics
- `GET /api/choreographedsaga/metrics` - Get saga metrics
- `DELETE /api/choreographedsaga/state/{sagaId}` - Delete saga state
- `GET /api/choreographedsaga/health` - Health check

### Choreographed Order API (`/api/orders`)
- `POST /api/orders/choreographed-saga` - Create order with choreographed saga pattern

### Saga State Management API (`/api/v1/saga-state`)
- `GET /api/v1/saga-state` - Get all sagas with their current states
- `GET /api/v1/saga-state/{sagaId}` - Get a specific saga by ID
- `GET /api/v1/saga-state/{sagaId}/transitions` - Get saga transitions (state history)
- `GET /api/v1/saga-state/by-state/{state}` - Get sagas by state
- `GET /api/v1/saga-state/events` - Get all saga events
- `GET /api/v1/saga-state/{sagaId}/events` - Get events for a specific saga

### Controlled Failure Management API (`/api/ControlledFailure`)
- `GET /api/ControlledFailure/config` - Get current failure configuration
- `PUT /api/ControlledFailure/config` - Update failure configuration
- `POST /api/ControlledFailure/toggle` - Enable or disable controlled failures
- `POST /api/ControlledFailure/probability` - Set failure probability for specific failure type
- `GET /api/ControlledFailure/affected-sagas` - Get sagas affected by failures
- `GET /api/ControlledFailure/compensation-stats` - Get compensation statistics
- `POST /api/ControlledFailure/simulate` - Simulate a specific failure type

### Saga Metrics & Monitoring API (`/api/SagaMetrics`)
- `GET /api/SagaMetrics/summary` - Get metrics summary
- `GET /api/SagaMetrics/prometheus` - Get Prometheus metrics in text format
- `GET /api/SagaMetrics/performance` - Get saga performance statistics
- `GET /api/SagaMetrics/state-distribution` - Get saga state distribution
- `GET /api/SagaMetrics/transition-analysis` - Get saga transition analysis
- `GET /api/SagaMetrics/duration-stats` - Get saga duration statistics
- `GET /api/SagaMetrics/recent-activity` - Get recent saga activity
- `GET /api/SagaMetrics/grafana` - Get Grafana integration information

## CORS Testing
- **CORS Test Page**: `http://cornershop.localhost/cors-test.html` - Test cross-origin requests

## 📚 Documentation
- [Technical Docs](docs/README.md)
- [UML Diagrams](docs/UML/)
- [Architecture Decision Records](docs/ADR/)
- [API Documentation](docs/API_README.md)
- [CORS Configuration](docs/CORS_README.md)
- [Load Testing Guide](load-tests/README.md)
- [Monitoring Setup](grafana/README.md)
- [Saga Orchestration](docs/SAGA_ORCHESTRATION.md)
- [Choreographed Saga Pattern](docs/CHOREOGRAPHED_SAGA_SEQUENCE.md)

## 🔄 Development Workflow

### Local Development
1. Start services: `./quick-start.sh`
2. Access application: `http://cornershop.localhost`
3. View logs: `docker-compose logs -f`
4. Stop services: `docker-compose down`

### Making Changes
1. Modify application code
2. Rebuild containers: `docker-compose up -d --build`
3. Test changes at `http://cornershop.localhost`

## 📊 Monitoring & Metrics

### Health Checks
- Application: `http://cornershop.localhost/health`
- Ready Check: `http://cornershop.localhost/health/ready`

### Metrics Endpoints
- Prometheus: `http://localhost:9090`
- Application Metrics: `http://cornershop.localhost/metrics`

## License
MIT