# CornerShop Shell Scripts Documentation

This document describes the shell scripts available in the CornerShop project and their relationship to microservices architecture.

## 🚀 Microservices Scripts

### Core Microservices Scripts

#### `start-microservices.sh` ✅
- **Purpose**: Starts the complete microservices architecture
- **Architecture**: Microservices (uses `docker-compose.microservices.yml`)
- **Features**:
  - Starts API Gateway, Product Service, Customer Service, Cart Service, Order Service
  - Includes MongoDB, Redis, Traefik, Grafana, Prometheus
  - Provides health checks for all services
  - Shows access URLs for all components

#### `test-microservices.sh` ✅ (NEW)
- **Purpose**: Comprehensive testing of microservices architecture
- **Architecture**: Microservices
- **Features**:
  - Tests individual service health
  - Tests API Gateway functionality and security
  - Tests service discovery via Traefik
  - Tests load balancing across cart service instances
  - Tests monitoring (Grafana, Prometheus)
  - Runs integration tests simulating user journeys

### Microservices Scripts

#### `quick-start.sh` ✅ (MODIFIED)
- **Purpose**: Quick setup and testing environment
- **Architecture**: Microservices only
- **Features**:
  - Starts microservices architecture
  - Shows microservices URLs
  - Installs dependencies (Docker, k6)

#### `run-load-tests.sh` ✅ (MODIFIED)
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