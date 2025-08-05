# CornerShop - Complete Instruction Documentation

## Table of Contents
1. [Getting Started](#getting-started)
2. [Development Setup](#development-setup)
3. [Usage Guide](#usage-guide)
4. [API Usage](#api-usage)
5. [Testing Instructions](#testing-instructions)
6. [Deployment Guide](#deployment-guide)
7. [Troubleshooting](#troubleshooting)
8. [Quick Start Guide](#quick-start-guide)
9. [Scripts Documentation](#scripts-documentation)
10. [Load Testing Instructions](#load-testing-instructions)
11. [Observability Setup](#observability-setup)
12. [Architecture Comparison Instructions](#architecture-comparison-instructions)

---

# Getting Started

## CornerShop - Multi-Store Management System

### What is CornerShop?
CornerShop is a distributed, web-based multi-store management system designed for retail businesses. It provides comprehensive inventory management, sales processing, and reporting capabilities with full offline operation support.

### Key Features
- **Multi-Store Support**: Each store operates independently with local data
- **Offline Operation**: Stores can operate without internet connectivity
- **Centralized Reporting**: Head office gets consolidated reports from all stores
- **REST API**: Full API support for integration and automation
- **Modern Web Interface**: Responsive design for desktop and mobile use

### System Requirements
- **Operating System**: Windows, macOS, or Linux
- **.NET 8.0**: Runtime and SDK
- **MongoDB**: For central database (optional for local operation)
- **SQLite**: For local store databases (included)
- **Docker**: For containerized deployment (optional)

---

# Development Setup

## Prerequisites

### Install Required Software
1. **.NET 8.0 SDK**
   ```bash
   # Download from https://dotnet.microsoft.com/download/dotnet/8.0
   # Verify installation
   dotnet --version
   ```

2. **MongoDB** (for central database)
   ```bash
   # Install MongoDB Community Edition
   # Start MongoDB service
   sudo systemctl start mongod
   ```

3. **Docker** (optional, for containerized deployment)
   ```bash
   # Install Docker
   sudo apt-get install docker.io
   sudo systemctl start docker
   ```

### Clone the Repository
```bash
git clone <repository-url>
cd LOG430_02_Lab7
```

## Local Development Setup

### 1. Database Setup
```bash
# Create local SQLite databases (automatic on first run)
# No additional setup required for local operation
```

### 2. Configuration
```bash
# Copy configuration template
cp appsettings.Development.json appsettings.json

# Edit configuration as needed
nano appsettings.json
```

### 3. Build and Run
```bash
# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

### 4. Verify Installation
- Open browser to `http://localhost:5000`
- You should see the CornerShop dashboard
- API documentation available at `http://localhost:5000/api-docs`

## Development Environment

### IDE Setup
**Visual Studio Code (Recommended)**
```bash
# Install VS Code extensions
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.vscode-dotnet-runtime
code --install-extension ms-vscode.vscode-json
```

**Visual Studio**
- Open the solution file
- Install recommended extensions
- Configure debugging settings

### Code Quality Tools
```bash
# Install code formatting tools
dotnet tool install -g dotnet-format

# Format code
dotnet format

# Run linting
dotnet build --verbosity normal
```

---

# Usage Guide

## Starting the Application

### Method 1: Direct Execution
```bash
# Navigate to project directory
cd LOG430_02_Lab7

# Run the application
dotnet run

# Application starts on http://localhost:5000
```

### Method 2: Docker (if available)
```bash
# Build Docker image
docker build -t cornershop .

# Run container
docker run -p 5000:80 cornershop
```

### Method 3: Development Mode
```bash
# Run with development configuration
dotnet run --environment Development

# Run with hot reload
dotnet watch run
```

## Web Interface Navigation

### Main Dashboard
1. **Home**: Overview and navigation hub
2. **Products**: Manage product inventory
3. **Sales**: Process sales and view history
4. **Stores**: Manage store locations
5. **Reports**: View consolidated reports

### Product Management

#### View Products
1. Navigate to **Products** section
2. Browse all products across stores
3. Use search and filter options
4. View product details by clicking on items

#### Add Product
1. Click **"Add Product"** button
2. Fill in product details:
   - Name
   - Category
   - Price
   - Store assignment
   - Stock quantity
   - Minimum stock level
3. Click **"Save"** to create product

#### Edit Product
1. Find the product in the list
2. Click **"Edit"** button
3. Modify product information
4. Click **"Save"** to update

#### Search Products
1. Use the search bar at the top
2. Enter product name or category
3. Results update automatically
4. Use advanced filters for specific criteria

### Sales Processing

#### Create Sale
1. Navigate to **Sales** section
2. Click **"New Sale"** button
3. Add items to cart:
   - Search for products
   - Select quantity
   - Add to cart
4. Review cart contents
5. Click **"Process Sale"** to complete

#### View Sales History
1. Go to **Sales** section
2. Browse recent sales by store
3. Click on sale for detailed view
4. Use date filters to find specific periods

#### Cancel Sale
1. Find the sale in history
2. Click **"Cancel"** button
3. Confirm cancellation
4. System will update inventory automatically

### Store Management

#### View Stores
1. Navigate to **Stores** section
2. See list of all store locations
3. View store status and details
4. Monitor store operations

#### Add Store
1. Click **"Add Store"** button
2. Enter store information:
   - Store name
   - Address
   - Contact information
   - Operating hours
3. Click **"Save"** to create store

#### Edit Store
1. Find store in the list
2. Click **"Edit"** button
3. Update store information
4. Click **"Save"** to update

### Reporting

#### Consolidated Reports
1. Navigate to **Reports** section
2. View sales across all stores
3. Analyze trends and patterns
4. Export data as needed

#### Inventory Reports
1. Check stock levels and values
2. Identify low stock items
3. Monitor inventory turnover
4. Generate reorder recommendations

#### Top Products
1. View best-selling items
2. Analyze product performance
3. Identify trends
4. Make inventory decisions

#### Sales Trends
1. Analyze sales patterns over time
2. Compare periods
3. Identify seasonal trends
4. Plan inventory accordingly

---

# API Usage

## API Access

### Base Information
- **Base URL**: `http://localhost:5000/api/v1/`
- **Documentation**: `http://localhost:5000/api-docs`
- **Alternative Docs**: `http://localhost:5000/redoc`
- **API Guide**: `http://localhost:5000/Home/ApiDocumentation`

### Authentication
Currently, no authentication is required. All endpoints are publicly accessible.

### Content Types
The API supports multiple formats:
- **JSON** (default): `application/json`
- **XML**: `application/xml` (via Accept header)

## API Endpoints

### Product Endpoints

#### Get All Products
```bash
curl http://localhost:5000/api/v1/products
```

#### Get Product by ID
```bash
curl http://localhost:5000/api/v1/products/1
```

#### Create a Product
```bash
curl -X POST http://localhost:5000/api/v1/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop",
    "category": "Electronics",
    "price": 999.99,
    "storeId": "store-123",
    "stockQuantity": 10,
    "minimumStockLevel": 2,
    "reorderPoint": 1
  }'
```

#### Update Product
```bash
curl -X PUT http://localhost:5000/api/v1/products/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Laptop",
    "price": 899.99,
    "stockQuantity": 15
  }'
```

#### Delete Product
```bash
curl -X DELETE http://localhost:5000/api/v1/products/1
```

#### Search Products
```bash
curl "http://localhost:5000/api/v1/products/search?searchTerm=laptop"
```

#### Get Products by Store
```bash
curl http://localhost:5000/api/v1/products/store/store-123
```

### Order Endpoints

#### Get All Orders
```bash
curl http://localhost:5000/api/v1/orders
```

#### Get Order by ID
```bash
curl http://localhost:5000/api/v1/orders/1
```

#### Create Order
```bash
curl -X POST http://localhost:5000/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "customer-123",
    "storeId": "store-123",
    "items": [
      {
        "productId": "product-1",
        "quantity": 2,
        "price": 999.99
      }
    ],
    "totalAmount": 1999.98
  }'
```

#### Update Order
```bash
curl -X PUT http://localhost:5000/api/v1/orders/1 \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Confirmed",
    "totalAmount": 1899.98
  }'
```

#### Cancel Order
```bash
curl -X DELETE http://localhost:5000/api/v1/orders/1
```

#### Get Customer Orders
```bash
curl http://localhost:5000/api/v1/orders/customer/customer-123
```

### Customer Endpoints

#### Get All Customers
```bash
curl http://localhost:5000/api/v1/customers
```

#### Get Customer by ID
```bash
curl http://localhost:5000/api/v1/customers/1
```

#### Create Customer
```bash
curl -X POST http://localhost:5000/api/v1/customers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "email": "john@example.com",
    "phone": "123-456-7890",
    "address": "123 Main St"
  }'
```

#### Update Customer
```bash
curl -X PUT http://localhost:5000/api/v1/customers/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Smith",
    "email": "johnsmith@example.com"
  }'
```

#### Delete Customer
```bash
curl -X DELETE http://localhost:5000/api/v1/customers/1
```

### Store Endpoints

#### Get All Stores
```bash
curl http://localhost:5000/api/v1/stores
```

#### Get Store by ID
```bash
curl http://localhost:5000/api/v1/stores/1
```

#### Create Store
```bash
curl -X POST http://localhost:5000/api/v1/stores \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Downtown Store",
    "address": "123 Main St",
    "phone": "123-456-7890",
    "email": "downtown@cornershop.com"
  }'
```

### Sales Endpoints

#### Get All Sales
```bash
curl http://localhost:5000/api/v1/sales
```

#### Get Sales by Store
```bash
curl http://localhost:5000/api/v1/sales/store/store-123
```

#### Get Sales by Date Range
```bash
curl "http://localhost:5000/api/v1/sales/range?startDate=2024-01-01&endDate=2024-01-31"
```

## API Response Format

### Success Response
```json
{
  "data": {
    "id": 1,
    "name": "Product Name",
    "price": 99.99
  },
  "links": [
    {
      "rel": "self",
      "href": "/api/v1/products/1"
    }
  ]
}
```

### Error Response
```json
{
  "error": {
    "message": "Product not found",
    "statusCode": 404,
    "timestamp": "2024-01-01T12:00:00Z",
    "path": "/api/v1/products/999"
  }
}
```

---

# Testing Instructions

## Running Tests

### Unit Tests
```bash
# Run all unit tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/CornerShop.Tests/
```

### Integration Tests
```bash
# Run integration tests
dotnet test tests/CornerShop.IntegrationTests/

# Run with specific configuration
dotnet test --environment Integration
```

### API Tests
```bash
# Run API tests
dotnet test tests/CornerShop.ApiTests/

# Test specific endpoints
dotnet test --filter "Category=API"
```

## Test Scripts

### Microservices Testing
```bash
# Run microservices test suite
./test-microservices.sh

# Test specific service
./test-microservices.sh --service product
```

### Saga Testing
```bash
# Test saga orchestration
./test-saga-orchestration.sh

# Test choreographed saga
./test-choreographed-saga.sh

# Test complete saga flow
./test-saga-complete.sh
```

### Load Testing
```bash
# Run load tests
./run-load-tests.sh

# Test specific scenario
./run-load-tests.sh --scenario peak-load
```

### Security Testing
```bash
# Test security features
./test-security-features.sh

# Test API gateway
./test-api-gateway.sh
```

## Test Data Setup

### Database Seeding
```bash
# Seed test data
dotnet run --project tools/DataSeeder/

# Seed specific data
dotnet run --project tools/DataSeeder/ -- --products --customers --stores
```

### Test Environment
```bash
# Set test environment
export ASPNETCORE_ENVIRONMENT=Test

# Run tests
dotnet test
```

---

# Deployment Guide

## Local Deployment

### Prerequisites
1. .NET 8.0 Runtime
2. MongoDB (optional)
3. Docker (optional)

### Method 1: Direct Deployment
```bash
# Build for production
dotnet publish -c Release -o ./publish

# Run production build
dotnet ./publish/CornerShop.dll
```

### Method 2: Docker Deployment
```bash
# Build Docker image
docker build -t cornershop:latest .

# Run container
docker run -d -p 5000:80 --name cornershop-app cornershop:latest

# View logs
docker logs cornershop-app
```

## Production Deployment

### Environment Setup
```bash
# Set production environment
export ASPNETCORE_ENVIRONMENT=Production

# Configure production settings
export ConnectionStrings__DefaultConnection="your-production-connection-string"
```

### Database Setup
```bash
# Run database migrations
dotnet ef database update

# Seed production data
dotnet run --project tools/DataSeeder/ --environment Production
```

### Reverse Proxy Setup (Nginx)
```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### SSL Configuration
```bash
# Install SSL certificate
sudo certbot --nginx -d your-domain.com

# Configure HTTPS redirect
```

## Monitoring Setup

### Health Checks
```bash
# Check application health
curl http://localhost:5000/health

# Check database health
curl http://localhost:5000/health/db
```

### Logging
```bash
# View application logs
tail -f logs/cornershop.log

# View system logs
journalctl -u cornershop -f
```

---

# Troubleshooting

## Common Issues

### Application Won't Start
```bash
# Check .NET version
dotnet --version

# Verify dependencies
dotnet restore

# Check port availability
netstat -tulpn | grep :5000
```

### Database Connection Issues
```bash
# Check MongoDB status
sudo systemctl status mongod

# Test connection
mongo --eval "db.runCommand('ping')"

# Check connection string
echo $ConnectionStrings__DefaultConnection
```

### API Endpoints Not Working
```bash
# Check API documentation
curl http://localhost:5000/api-docs

# Test basic endpoint
curl http://localhost:5000/api/v1/products

# Check CORS configuration
```

### Performance Issues
```bash
# Check resource usage
top
htop

# Monitor database performance
# Check application logs for errors
```

## Debug Mode

### Enable Debug Logging
```bash
# Set debug level
export Logging__LogLevel__Default=Debug

# Run with debug information
dotnet run --verbosity detailed
```

### Database Debugging
```bash
# Enable SQL logging
export Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information

# Check database queries
```

---

# Quick Start Guide

## 5-Minute Setup

### Step 1: Clone and Build
```bash
git clone <repository-url>
cd LOG430_02_Lab7
dotnet restore
dotnet build
```

### Step 2: Run Application
```bash
dotnet run
```

### Step 3: Access Application
- Open browser to `http://localhost:5000`
- Explore the dashboard
- Check API docs at `http://localhost:5000/api-docs`

### Step 4: Create Test Data
```bash
# Add a store
curl -X POST http://localhost:5000/api/v1/stores \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Store", "address": "123 Test St"}'

# Add a product
curl -X POST http://localhost:5000/api/v1/products \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Product", "price": 99.99, "storeId": "1"}'
```

## Quick Commands Reference

### Development
```bash
dotnet run                    # Run application
dotnet watch run             # Run with hot reload
dotnet test                  # Run tests
dotnet build                 # Build project
dotnet clean                 # Clean build artifacts
```

### Docker
```bash
docker build -t cornershop . # Build image
docker run -p 5000:80 cornershop # Run container
docker-compose up            # Run with compose
```

### Testing
```bash
./test-microservices.sh      # Test microservices
./test-saga-complete.sh      # Test saga flow
./run-load-tests.sh          # Run load tests
```

---

# Scripts Documentation

## Overview
This section describes all available shell scripts in the CornerShop project and their relationship to the microservices architecture. These scripts provide comprehensive testing, deployment, and management capabilities for the distributed system.

## 🚀 Core Microservices Scripts

### `start-microservices.sh` ✅
- **Purpose**: Starts the complete microservices architecture
- **Architecture**: Microservices (uses `docker-compose.microservices.yml`)
- **Features**:
  - Starts API Gateway, Product Service, Customer Service, Cart Service, Order Service
  - Includes MongoDB, Redis, Traefik, Grafana, Prometheus
  - Provides health checks for all services
  - Shows access URLs for all components

### `test-microservices.sh` ✅
- **Purpose**: Comprehensive testing of microservices architecture
- **Architecture**: Microservices
- **Features**:
  - Tests individual service health
  - Tests API Gateway functionality and security
  - Tests service discovery via Traefik
  - Tests load balancing across cart service instances
  - Tests monitoring (Grafana, Prometheus)
  - Runs integration tests simulating user journeys

### `quick-start.sh` ✅
- **Purpose**: Quick setup and testing environment
- **Architecture**: Microservices only
- **Features**:
  - Starts microservices architecture
  - Shows microservices URLs
  - Installs dependencies (Docker, k6)

### `run-load-tests.sh` ✅
- **Purpose**: Load testing suite
- **Architecture**: Microservices only
- **Features**:
  - Tests individual microservices
  - Provides microservices access information
  - Comprehensive load testing for microservices architecture

## 🧪 Microservices-Specific Test Scripts

### API Gateway Testing
- **`test-api-gateway.sh`** ✅ - Tests API Gateway functionality and security
- **`test-gateway-functionality.sh`** ✅ - Comprehensive gateway testing

### Load Balancing Testing
- **`test-load-balancing.sh`** ✅ - Tests round-robin load balancing for cart service

### Saga Pattern Testing
- **`test-saga-orchestration.sh`** ✅ - Tests distributed transaction orchestration
- **`test-saga-state-machine.sh`** ✅ - Tests saga state machine functionality
- **`test-saga-complete.sh`** ✅ - End-to-end saga testing

### Business Event Testing
- **`test-business-event-producers.sh`** ✅ - Tests event-driven communication
- **`test-controlled-failures.sh`** ✅ - Tests fault tolerance and recovery

### Monitoring and Observability
- **`test-saga-metrics-monitoring.sh`** ✅ - Tests metrics collection and monitoring
- **`run-observability-comparison.sh`** ✅ - Compares observability between architectures

### Security Testing
- **`test-security-features.sh`** ✅ - Tests security features across microservices

## 🏗️ Performance Analysis Scripts

### Performance and Observability Analysis
- **`run-architecture-comparison.sh`** ✅ - Analyzes microservices performance
- **`run-observability-comparison.sh`** ✅ - Analyzes observability capabilities

## 🔧 Utility Scripts

### Code Quality
- **`lint.sh`** ✅ - Lints code across all services

## 📊 Usage Examples

### Starting Microservices
```bash
# Start microservices architecture
./start-microservices.sh

# Quick start (auto-detects architecture)
./quick-start.sh
```

### Testing Microservices
```bash
# Comprehensive microservices testing
./test-microservices.sh

# Test specific components
./test-api-gateway.sh
./test-load-balancing.sh
./test-saga-orchestration.sh
```

### Load Testing
```bash
# Load testing (auto-detects architecture)
./run-load-tests.sh

# With additional stress testing
./run-load-tests.sh --stress
```

### Performance Analysis
```bash
# Analyze microservices performance
./run-architecture-comparison.sh
./run-observability-comparison.sh
```

## 🌐 Access URLs

### Microservices Architecture
- **API Gateway**: http://api.cornershop.localhost
- **Product Service**: http://product.cornershop.localhost
- **Customer Service**: http://customer.cornershop.localhost
- **Cart Service**: http://cart.cornershop.localhost
- **Order Service**: http://order.cornershop.localhost
- **Traefik Dashboard**: http://traefik.localhost:8080
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090

## 🔑 API Authentication

For microservices architecture, use the API key:
```
X-API-Key: cornershop-api-key-2024
```

## Script Configuration

### Environment Variables
```bash
# Set API key
export API_KEY="cornershop-api-key-2024"

# Set base URL
export BASE_URL="http://localhost"

# Set timeout
export TIMEOUT=30
```

### Configuration Files
```bash
# Test configuration
test-config.json

# Load test configuration
load-test-config.json

# Environment configuration
.env
```

## 📋 Script Status Summary

| Script | Microservices | Status |
|--------|---------------|---------|
| `start-microservices.sh` | ✅ | Ready |
| `test-microservices.sh` | ✅ | Ready |
| `quick-start.sh` | ✅ | Modified |
| `run-load-tests.sh` | ✅ | Modified |
| `test-api-gateway.sh` | ✅ | Ready |
| `test-load-balancing.sh` | ✅ | Ready |
| `test-saga-*.sh` | ✅ | Ready |
| `test-business-event-*.sh` | ✅ | Ready |
| `test-security-features.sh` | ✅ | Ready |
| `run-architecture-comparison.sh` | ✅ | Ready |
| `run-observability-comparison.sh` | ✅ | Ready |
| `lint.sh` | ✅ | Ready |

## 🚨 Important Notes

1. **All scripts require the microservices architecture to be running**
2. **API Gateway is required for testing**
3. **All scripts use the API key for authentication**
4. **Load balancing tests require multiple cart service instances**
5. **Use `start-microservices.sh` to start the architecture**

---

# Load Testing Instructions

## Load Testing Setup

### Prerequisites
```bash
# Install Artillery
npm install -g artillery

# Install JMeter (optional)
sudo apt-get install jmeter
```

### Basic Load Test
```bash
# Run basic load test
artillery run load-tests/basic-load.yml

# Test specific endpoint
artillery run load-tests/api-load.yml
```

### Advanced Load Testing

#### Peak Load Test
```bash
# Simulate peak load
artillery run load-tests/peak-load.yml

# Monitor system performance
htop
```

#### Stress Test
```bash
# Find system breaking point
artillery run load-tests/stress-test.yml

# Monitor error rates
```

#### Endurance Test
```bash
# Test system stability over time
artillery run load-tests/endurance-test.yml

# Run for extended period
artillery run load-tests/endurance-test.yml --duration 3600
```

### Load Test Scenarios

#### Product Catalog Load
```bash
# Test product listing
artillery run load-tests/product-catalog.yml

# Test product search
artillery run load-tests/product-search.yml
```

#### Order Processing Load
```bash
# Test order creation
artillery run load-tests/order-creation.yml

# Test order retrieval
artillery run load-tests/order-retrieval.yml
```

#### User Registration Load
```bash
# Test user registration
artillery run load-tests/user-registration.yml

# Test user authentication
artillery run load-tests/user-authentication.yml
```

### Performance Metrics

#### Response Time Analysis
```bash
# Analyze response times
artillery report load-test-results.json

# Generate detailed report
artillery report --output report.html load-test-results.json
```

#### Throughput Analysis
```bash
# Calculate requests per second
artillery report --output throughput.json load-test-results.json

# Analyze throughput patterns
```

### Load Test Results

#### Understanding Results
- **Response Time**: Average, 95th percentile, 99th percentile
- **Throughput**: Requests per second
- **Error Rate**: Percentage of failed requests
- **Resource Usage**: CPU, memory, network utilization

#### Performance Benchmarks
- **Acceptable Response Time**: < 200ms for 95% of requests
- **Target Throughput**: > 1000 requests/second
- **Error Rate**: < 1%
- **Resource Utilization**: < 80% CPU, < 80% memory

---

# Observability Setup

## Monitoring Stack

### Prometheus Setup
```bash
# Start Prometheus
docker run -d -p 9090:9090 prom/prometheus

# Configure Prometheus
# Edit prometheus.yml with your targets
```

### Grafana Setup
```bash
# Start Grafana
docker run -d -p 3000:3000 grafana/grafana

# Access Grafana
# Open http://localhost:3000
# Default credentials: admin/admin
```

### Application Metrics
```bash
# Enable metrics endpoint
curl http://localhost:5000/metrics

# View Prometheus metrics
curl http://localhost:5000/metrics | grep http_requests_total
```

## Logging Setup

### Structured Logging
```bash
# Configure logging
export Logging__LogLevel__Default=Information
export Logging__LogLevel__Microsoft=Warning
```

### Log Aggregation
```bash
# Send logs to centralized system
# Configure log shipping
```

## Health Checks

### Application Health
```bash
# Check overall health
curl http://localhost:5000/health

# Check specific components
curl http://localhost:5000/health/db
curl http://localhost:5000/health/redis
```

### Service Health
```bash
# Check individual services
curl http://localhost:5000/health/products
curl http://localhost:5000/health/orders
curl http://localhost:5000/health/customers
```

## Alerting Setup

### Prometheus Alerts
```yaml
# Configure alerting rules
groups:
  - name: cornershop
    rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: High error rate detected
```

### Grafana Dashboards
```bash
# Import dashboards
# Use provided dashboard JSON files
```

---

# Architecture Comparison Instructions

## Running Architecture Comparison

### Setup Comparison Environment
```bash
# Start both architectures
./start-monolithic.sh
./start-microservices.sh

# Wait for services to be ready
sleep 30
```

### Run Comparison Tests
```bash
# Run comprehensive comparison
./run-architecture-comparison.sh

# Test specific aspects
./run-architecture-comparison.sh --performance
./run-architecture-comparison.sh --scalability
./run-architecture-comparison.sh --reliability
```

### Performance Comparison
```bash
# Test response times
artillery run comparison-tests/response-time.yml

# Test throughput
artillery run comparison-tests/throughput.yml

# Test resource usage
./monitor-resources.sh
```

### Scalability Comparison
```bash
# Test horizontal scaling
./test-scaling.sh --architecture monolithic
./test-scaling.sh --architecture microservices

# Compare scaling results
./compare-scaling-results.sh
```

### Reliability Comparison
```bash
# Test fault tolerance
./test-fault-tolerance.sh --architecture monolithic
./test-fault-tolerance.sh --architecture microservices

# Compare reliability metrics
./compare-reliability-results.sh
```

## Comparison Metrics

### Performance Metrics
- **Response Time**: Average, 95th percentile, 99th percentile
- **Throughput**: Requests per second
- **Latency**: Network and processing latency
- **Resource Usage**: CPU, memory, network utilization

### Scalability Metrics
- **Horizontal Scaling**: Performance with multiple instances
- **Vertical Scaling**: Performance with increased resources
- **Load Distribution**: Evenness of load distribution
- **Bottleneck Identification**: System bottlenecks under load

### Reliability Metrics
- **Fault Tolerance**: System behavior during failures
- **Recovery Time**: Time to recover from failures
- **Error Handling**: Quality of error responses
- **Data Consistency**: Consistency during failures

## Generating Reports

### Performance Report
```bash
# Generate performance comparison
./generate-performance-report.sh

# View report
open reports/performance-comparison.html
```

### Scalability Report
```bash
# Generate scalability comparison
./generate-scalability-report.sh

# View report
open reports/scalability-comparison.html
```

### Reliability Report
```bash
# Generate reliability comparison
./generate-reliability-report.sh

# View report
open reports/reliability-comparison.html
```

---

# Conclusion

This comprehensive instruction documentation provides complete guidance for setting up, using, testing, and deploying the CornerShop system. The documentation covers all aspects from initial setup to advanced testing and monitoring.

## Key Takeaways

1. **Easy Setup**: The system can be set up quickly with minimal configuration
2. **Comprehensive Testing**: Multiple testing approaches ensure system reliability
3. **Flexible Deployment**: Support for various deployment scenarios
4. **Rich API**: Full REST API with comprehensive documentation
5. **Monitoring Ready**: Built-in observability and monitoring capabilities

## Next Steps

1. **Start with Quick Start**: Use the 5-minute setup guide
2. **Explore the API**: Test the REST API endpoints
3. **Run Tests**: Execute the comprehensive test suite
4. **Monitor Performance**: Set up observability and monitoring
5. **Scale as Needed**: Use the architecture comparison tools

The CornerShop system is designed to be developer-friendly, production-ready, and scalable for various business needs. 