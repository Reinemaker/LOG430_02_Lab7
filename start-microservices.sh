#!/bin/bash

echo "🧹 Cleaning up Docker resources to free up space..."
docker system prune -a -f --volumes

echo "🚀 Starting CornerShop Microservices Architecture..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker first."
    exit 1
fi

# Check if docker-compose.microservices.yml exists
if [ ! -f "docker-compose.microservices.yml" ]; then
    echo "❌ docker-compose.microservices.yml not found. Please ensure you're in the correct directory."
    exit 1
fi

echo "📦 Building and starting microservices..."

# Build and start all services
docker compose -f docker-compose.microservices.yml up -d --build

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 30

# Check service health
echo "🔍 Checking service health..."

# Check if services are responding
services=(
    "product-service"
    "customer-service"
    "cart-service"
    "order-service"
    "mongodb"
    "redis"
)

for service in "${services[@]}"; do
    if docker compose -f docker-compose.microservices.yml ps | grep -q "$service.*Up"; then
        echo "✅ $service is running"
    else
        echo "❌ $service is not running"
    fi
done

echo ""
echo "🎉 CornerShop Microservices with API Gateway are now running!"
echo ""
echo "🌐 API Gateway (Main Entry Point):"
echo "   • URL:                 http://api.cornershop.localhost"
echo "   • API Key Required:    X-API-Key: cornershop-api-key-2024"
echo ""
echo "📋 Individual Service URLs:"
echo "   • Product Service:     http://product.cornershop.localhost"
echo "   • Customer Service:    http://customer.cornershop.localhost"
echo "   • Cart Service:        http://cart.cornershop.localhost"
echo "   • Order Service:       http://order.cornershop.localhost"
echo "   • Traefik Dashboard:   http://traefik.localhost:8080"
echo "   • Grafana:             http://localhost:3000 (admin/admin)"
echo "   • Prometheus:          http://localhost:9090"
echo ""
echo "📚 API Documentation:"
echo "   • Product Service:     http://product.cornershop.localhost/swagger"
echo "   • Customer Service:    http://customer.cornershop.localhost/swagger"
echo "   • Cart Service:        http://cart.cornershop.localhost/swagger"
echo "   • Order Service:       http://order.cornershop.localhost/swagger"
echo ""
echo "🔧 Health Checks:"
echo "   • Product Service:     http://product.cornershop.localhost/health"
echo "   • Customer Service:    http://customer.cornershop.localhost/health"
echo "   • Cart Service:        http://cart.cornershop.localhost/health"
echo "   • Order Service:       http://order.cornershop.localhost/health"
echo ""
echo "📊 Monitoring:"
echo "   • Grafana Dashboards:  http://localhost:3000"
echo "   • Prometheus Metrics:  http://localhost:9090"
echo ""
echo "🛑 To stop all services:"
echo "   docker compose -f docker-compose.microservices.yml down"
echo ""
echo ""
echo "📝 Example API Gateway Calls:"
echo "   • Get Products:        curl -H 'X-API-Key: cornershop-api-key-2024' http://api.cornershop.localhost/api/products"
echo "   • Get Customers:       curl -H 'X-API-Key: cornershop-api-key-2024' http://api.cornershop.localhost/api/customers"
echo "   • Health Check:        curl http://api.cornershop.localhost/health"
echo ""
echo "📖 For more information, see README-Microservices.md and API_GATEWAY_IMPLEMENTATION.md" 