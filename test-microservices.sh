#!/bin/bash

# CornerShop Microservices Testing Script
# This script tests all microservices individually and their interactions

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_GATEWAY_URL="http://api.cornershop.localhost"
API_KEY="cornershop-api-key-2024"
SERVICES=("product" "customer" "cart" "order")
TIMEOUT=10

echo -e "${BLUE}=== CornerShop Microservices Testing Suite ===${NC}"
echo ""

# Function to test service health
test_service_health() {
    local service=$1
    local url="http://${service}.cornershop.localhost"
    
    echo -e "${YELLOW}Testing ${service}-service health...${NC}"
    
    if curl -f -s --max-time $TIMEOUT "${url}/health" > /dev/null; then
        echo -e "${GREEN}✅ ${service}-service is healthy${NC}"
        return 0
    else
        echo -e "${RED}❌ ${service}-service is not responding${NC}"
        return 1
    fi
}

# Function to test service API endpoints
test_service_api() {
    local service=$1
    local url="http://${service}.cornershop.localhost"
    
    echo -e "${YELLOW}Testing ${service}-service API endpoints...${NC}"
    
    case $service in
        "product")
            # Test product endpoints
            if curl -f -s --max-time $TIMEOUT "${url}/api/products" > /dev/null; then
                echo -e "${GREEN}✅ GET /api/products - OK${NC}"
            else
                echo -e "${RED}❌ GET /api/products - Failed${NC}"
            fi
            
            if curl -f -s --max-time $TIMEOUT "${url}/api/products/1" > /dev/null; then
                echo -e "${GREEN}✅ GET /api/products/1 - OK${NC}"
            else
                echo -e "${RED}❌ GET /api/products/1 - Failed${NC}"
            fi
            ;;
        "customer")
            # Test customer endpoints
            if curl -f -s --max-time $TIMEOUT "${url}/api/customers" > /dev/null; then
                echo -e "${GREEN}✅ GET /api/customers - OK${NC}"
            else
                echo -e "${RED}❌ GET /api/customers - Failed${NC}"
            fi
            ;;
        "cart")
            # Test cart endpoints
            if curl -f -s --max-time $TIMEOUT "${url}/api/cart" > /dev/null; then
                echo -e "${GREEN}✅ GET /api/cart - OK${NC}"
            else
                echo -e "${RED}❌ GET /api/cart - Failed${NC}"
            fi
            ;;
        "order")
            # Test order endpoints
            if curl -f -s --max-time $TIMEOUT "${url}/api/orders" > /dev/null; then
                echo -e "${GREEN}✅ GET /api/orders - OK${NC}"
            else
                echo -e "${RED}❌ GET /api/orders - Failed${NC}"
            fi
            ;;
    esac
}

# Function to test API Gateway
test_api_gateway() {
    echo -e "${BLUE}=== Testing API Gateway ===${NC}"
    
    # Test API Gateway health
    if curl -f -s --max-time $TIMEOUT "${API_GATEWAY_URL}/health" > /dev/null; then
        echo -e "${GREEN}✅ API Gateway health check - OK${NC}"
    else
        echo -e "${RED}❌ API Gateway health check - Failed${NC}"
        return 1
    fi
    
    # Test API Gateway routing to services
    echo -e "${YELLOW}Testing API Gateway routing...${NC}"
    
    # Test product service through gateway
    if curl -f -s --max-time $TIMEOUT -H "X-API-Key: ${API_KEY}" "${API_GATEWAY_URL}/api/products" > /dev/null; then
        echo -e "${GREEN}✅ API Gateway -> Product Service - OK${NC}"
    else
        echo -e "${RED}❌ API Gateway -> Product Service - Failed${NC}"
    fi
    
    # Test customer service through gateway
    if curl -f -s --max-time $TIMEOUT -H "X-API-Key: ${API_KEY}" "${API_GATEWAY_URL}/api/customers" > /dev/null; then
        echo -e "${GREEN}✅ API Gateway -> Customer Service - OK${NC}"
    else
        echo -e "${RED}❌ API Gateway -> Customer Service - Failed${NC}"
    fi
    
    # Test cart service through gateway
    if curl -f -s --max-time $TIMEOUT -H "X-API-Key: ${API_KEY}" "${API_GATEWAY_URL}/api/cart" > /dev/null; then
        echo -e "${GREEN}✅ API Gateway -> Cart Service - OK${NC}"
    else
        echo -e "${RED}❌ API Gateway -> Cart Service - Failed${NC}"
    fi
    
    # Test order service through gateway
    if curl -f -s --max-time $TIMEOUT -H "X-API-Key: ${API_KEY}" "${API_GATEWAY_URL}/api/orders" > /dev/null; then
        echo -e "${GREEN}✅ API Gateway -> Order Service - OK${NC}"
    else
        echo -e "${RED}❌ API Gateway -> Order Service - Failed${NC}"
    fi
}

# Function to test API Gateway security
test_api_gateway_security() {
    echo -e "${BLUE}=== Testing API Gateway Security ===${NC}"
    
    # Test without API key (should fail)
    echo -e "${YELLOW}Testing without API key...${NC}"
    response=$(curl -s -w "%{http_code}" "${API_GATEWAY_URL}/api/products" -o /dev/null)
    if [ "$response" = "401" ]; then
        echo -e "${GREEN}✅ API Gateway correctly rejects requests without API key${NC}"
    else
        echo -e "${RED}❌ API Gateway should reject requests without API key (got $response)${NC}"
    fi
    
    # Test with invalid API key (should fail)
    echo -e "${YELLOW}Testing with invalid API key...${NC}"
    response=$(curl -s -w "%{http_code}" -H "X-API-Key: invalid-key" "${API_GATEWAY_URL}/api/products" -o /dev/null)
    if [ "$response" = "403" ]; then
        echo -e "${GREEN}✅ API Gateway correctly rejects requests with invalid API key${NC}"
    else
        echo -e "${RED}❌ API Gateway should reject requests with invalid API key (got $response)${NC}"
    fi
}

# Function to test service discovery
test_service_discovery() {
    echo -e "${BLUE}=== Testing Service Discovery ===${NC}"
    
    # Check if Traefik is running
    if curl -f -s --max-time $TIMEOUT "http://traefik.localhost:8080" > /dev/null; then
        echo -e "${GREEN}✅ Traefik dashboard is accessible${NC}"
        
        # Check if services are registered with Traefik
        echo -e "${YELLOW}Checking service registration...${NC}"
        for service in "${SERVICES[@]}"; do
            if curl -f -s --max-time $TIMEOUT "http://${service}.cornershop.localhost" > /dev/null; then
                echo -e "${GREEN}✅ ${service}-service is discoverable${NC}"
            else
                echo -e "${RED}❌ ${service}-service is not discoverable${NC}"
            fi
        done
    else
        echo -e "${YELLOW}⚠ Traefik dashboard not accessible${NC}"
    fi
}

# Function to test load balancing
test_load_balancing() {
    echo -e "${BLUE}=== Testing Load Balancing ===${NC}"
    
    # Test cart service load balancing (should have multiple instances)
    echo -e "${YELLOW}Testing cart service load balancing...${NC}"
    
    local requests=10
    local instances=()
    
    for i in $(seq 1 $requests); do
        response=$(curl -s --max-time $TIMEOUT "http://cart.cornershop.localhost/health")
        instance=$(echo "$response" | grep -o '"instance":[^,}]*' | cut -d':' -f2 | tr -d '"' || echo "unknown")
        instances+=("$instance")
        echo -n "."
    done
    
    echo ""
    
    # Count unique instances
    unique_instances=$(printf '%s\n' "${instances[@]}" | sort -u | wc -l)
    echo -e "${YELLOW}Requests distributed across $unique_instances instance(s)${NC}"
    
    if [ "$unique_instances" -gt 1 ]; then
        echo -e "${GREEN}✅ Load balancing is working${NC}"
    else
        echo -e "${YELLOW}⚠ Load balancing may not be working (only 1 instance detected)${NC}"
    fi
}

# Function to test monitoring
test_monitoring() {
    echo -e "${BLUE}=== Testing Monitoring ===${NC}"
    
    # Check Grafana
    if curl -f -s --max-time $TIMEOUT "http://localhost:3000" > /dev/null; then
        echo -e "${GREEN}✅ Grafana is accessible${NC}"
    else
        echo -e "${YELLOW}⚠ Grafana not accessible${NC}"
    fi
    
    # Check Prometheus
    if curl -f -s --max-time $TIMEOUT "http://localhost:9090" > /dev/null; then
        echo -e "${GREEN}✅ Prometheus is accessible${NC}"
    else
        echo -e "${YELLOW}⚠ Prometheus not accessible${NC}"
    fi
}

# Function to run integration test
run_integration_test() {
    echo -e "${BLUE}=== Running Integration Test ===${NC}"
    
    # Simulate a complete user journey
    echo -e "${YELLOW}Simulating user journey: Browse products -> Add to cart -> Place order${NC}"
    
    # 1. Browse products
    echo -e "${YELLOW}1. Browsing products...${NC}"
    if curl -f -s --max-time $TIMEOUT -H "X-API-Key: ${API_KEY}" "${API_GATEWAY_URL}/api/products" > /dev/null; then
        echo -e "${GREEN}✅ Product browsing - OK${NC}"
    else
        echo -e "${RED}❌ Product browsing - Failed${NC}"
        return 1
    fi
    
    # 2. Add item to cart
    echo -e "${YELLOW}2. Adding item to cart...${NC}"
    cart_response=$(curl -s --max-time $TIMEOUT -H "X-API-Key: ${API_KEY}" \
        -H "Content-Type: application/json" \
        -d '{"productId": 1, "quantity": 2}' \
        "${API_GATEWAY_URL}/api/cart/add")
    
    if echo "$cart_response" | grep -q "success\|added"; then
        echo -e "${GREEN}✅ Add to cart - OK${NC}"
    else
        echo -e "${RED}❌ Add to cart - Failed${NC}"
        return 1
    fi
    
    # 3. Place order
    echo -e "${YELLOW}3. Placing order...${NC}"
    order_response=$(curl -s --max-time $TIMEOUT -H "X-API-Key: ${API_KEY}" \
        -H "Content-Type: application/json" \
        -d '{"customerId": 1, "items": [{"productId": 1, "quantity": 2}]}' \
        "${API_GATEWAY_URL}/api/orders")
    
    if echo "$order_response" | grep -q "order\|created"; then
        echo -e "${GREEN}✅ Place order - OK${NC}"
    else
        echo -e "${RED}❌ Place order - Failed${NC}"
        return 1
    fi
    
    echo -e "${GREEN}✅ Integration test completed successfully!${NC}"
}

# Main execution
main() {
    echo -e "${BLUE}Starting microservices testing...${NC}"
    echo ""
    
    # Check if microservices are running
    if ! curl -f -s --max-time $TIMEOUT "${API_GATEWAY_URL}/health" > /dev/null; then
        echo -e "${RED}❌ API Gateway is not accessible${NC}"
        echo -e "${YELLOW}Make sure microservices are running: ./start-microservices.sh${NC}"
        exit 1
    fi
    
    # Test individual services
    echo -e "${BLUE}=== Testing Individual Services ===${NC}"
    for service in "${SERVICES[@]}"; do
        echo ""
        test_service_health "$service"
        test_service_api "$service"
    done
    
    echo ""
    
    # Test API Gateway
    test_api_gateway
    
    echo ""
    
    # Test security
    test_api_gateway_security
    
    echo ""
    
    # Test service discovery
    test_service_discovery
    
    echo ""
    
    # Test load balancing
    test_load_balancing
    
    echo ""
    
    # Test monitoring
    test_monitoring
    
    echo ""
    
    # Run integration test
    run_integration_test
    
    echo ""
    echo -e "${GREEN}=== Microservices Testing Completed! ===${NC}"
    echo ""
    echo -e "${BLUE}=== Access URLs ===${NC}"
    echo -e "${YELLOW}API Gateway: ${API_GATEWAY_URL}${NC}"
    echo -e "${YELLOW}Traefik Dashboard: http://traefik.localhost:8080${NC}"
    echo -e "${YELLOW}Grafana Dashboard: http://localhost:3000 (admin/admin)${NC}"
    echo -e "${YELLOW}Prometheus: http://localhost:9090${NC}"
    echo ""
}

# Handle command line arguments
case "$1" in
    "--help"|"-h")
        echo "Usage: $0 [OPTIONS]"
        echo ""
        echo "Options:"
        echo "  --help, -h           Show this help message"
        echo ""
        echo "This script will:"
        echo "  1. Test individual microservices health"
        echo "  2. Test API Gateway functionality"
        echo "  3. Test security features"
        echo "  4. Test service discovery"
        echo "  5. Test load balancing"
        echo "  6. Test monitoring"
        echo "  7. Run integration tests"
        echo ""
        ;;
    *)
        main
        ;;
esac 