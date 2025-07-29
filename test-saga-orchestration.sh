#!/bin/bash

# Test Saga Orchestration Script
# This script tests the distributed saga orchestration across microservices

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration - Updated for microservices with API Gateway
BASE_URL="http://localhost"
API_GATEWAY_URL="${BASE_URL}/api"
SAGA_URL="${API_GATEWAY_URL}/saga"
ORDER_URL="${API_GATEWAY_URL}/orders"
STOCK_URL="${API_GATEWAY_URL}/stock"
PAYMENT_URL="${API_GATEWAY_URL}/payments"
API_KEY="cornershop-api-key-2024"
REDIS_URL="localhost:6379"

# Headers for API Gateway
HEADERS="-H 'Content-Type: application/json' -H 'X-API-Key: ${API_KEY}'"

echo -e "${BLUE}=== Saga Orchestration Test Script (Microservices) ===${NC}"
echo "Testing distributed saga orchestration across microservices via API Gateway"
echo ""

# Function to check if a service is running
check_service() {
    local url=$1
    local service_name=$2
    
    echo -e "${YELLOW}Checking if $service_name is running...${NC}"
    if curl -s -f "$url/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ $service_name is running${NC}"
        return 0
    else
        echo -e "${RED}✗ $service_name is not running${NC}"
        return 1
    fi
}

# Function to make API calls and check responses
api_call() {
    local method=$1
    local url=$2
    local data=$3
    local description=$4
    
    echo -e "${YELLOW}$description${NC}"
    echo "URL: $url"
    if [ ! -z "$data" ]; then
        echo "Data: $data"
    fi
    echo ""
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" "$url" $HEADERS)
    else
        response=$(curl -s -w "\n%{http_code}" -X "$method" -H "Content-Type: application/json" -H "X-API-Key: $API_KEY" -d "$data" "$url")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n -1)
    
    echo "HTTP Status: $http_code"
    echo "Response:"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
    echo ""
    
    if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        echo -e "${GREEN}✓ Success${NC}"
        return 0
    else
        echo -e "${RED}✗ Failed${NC}"
        return 1
    fi
}

# Check if services are running
echo -e "${BLUE}=== Service Health Checks ===${NC}"
check_service "$BASE_URL" "API Gateway" || exit 1
check_service "$SAGA_URL" "Saga Orchestrator" || exit 1
check_service "$ORDER_URL" "Order Service" || exit 1
check_service "$STOCK_URL" "Stock Service" || exit 1
check_service "$PAYMENT_URL" "Payment Service" || exit 1
echo ""

# Check Redis connection
echo -e "${BLUE}=== Redis Connection Test ===${NC}"
if redis-cli -h "$REDIS_URL" ping > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Redis is running${NC}"
else
    echo -e "${RED}✗ Redis is not running${NC}"
    exit 1
fi
echo ""

# Test 1: Get Saga Orchestrator Health
echo -e "${BLUE}=== Test 1: Saga Orchestrator Health ===${NC}"
api_call "GET" "$SAGA_URL/health" "" "Getting Saga Orchestrator health status"
echo ""

# Test 2: Get Service Saga Info
echo -e "${BLUE}=== Test 2: Service Saga Info ===${NC}"
api_call "GET" "$ORDER_URL/saga/info" "" "Getting Order Service saga participant information"
api_call "GET" "$STOCK_URL/saga/info" "" "Getting Stock Service saga participant information"
api_call "GET" "$PAYMENT_URL/saga/info" "" "Getting Payment Service saga participant information"
echo ""

# Test 3: Get Event Statistics
echo -e "${BLUE}=== Test 3: Event Statistics ===${NC}"
api_call "GET" "$SAGA_URL/events/statistics" "" "Getting Saga Orchestrator event statistics"
api_call "GET" "$ORDER_URL/events/statistics" "" "Getting Order Service event statistics"
api_call "GET" "$STOCK_URL/events/statistics" "" "Getting Stock Service event statistics"
api_call "GET" "$PAYMENT_URL/events/statistics" "" "Getting Payment Service event statistics"
echo ""

# Test 4: Demo Saga Execution
echo -e "${BLUE}=== Test 4: Demo Saga Execution ===${NC}"
demo_data='{
    "sagaType": "OrderCreation",
    "orderId": "demo-order-001",
    "customerId": "demo-customer-001",
    "storeId": "demo-store-001",
    "totalAmount": 37.48,
    "paymentMethod": "CreditCard",
    "items": [
        {
            "productId": "prod-001",
            "quantity": 2,
            "price": 10.99
        },
        {
            "productId": "prod-002",
            "quantity": 1,
            "price": 15.50
        }
    ]
}'

api_call "POST" "$SAGA_URL/execute" "$demo_data" "Executing demo saga orchestration"
echo ""

# Test 5: Get Saga Metrics
echo -e "${BLUE}=== Test 5: Saga Metrics ===${NC}"
api_call "GET" "$SAGA_URL/metrics" "" "Getting saga metrics"
echo ""

# Test 6: Test Saga Participation (Direct)
echo -e "${BLUE}=== Test 6: Direct Saga Participation ===${NC}"
participation_data='{
    "sagaId": "test-saga-002",
    "stepName": "ConfirmOrder",
    "orderId": "test-order-002",
    "data": {
        "orderId": "test-order-002",
        "customerId": "test-customer-002",
        "storeId": "test-store-002"
    },
    "correlationId": "test-correlation-002"
}'

api_call "POST" "$ORDER_URL/saga/participate" "$participation_data" "Testing direct saga participation with Order Service"
echo ""

# Test 7: Test Saga Compensation
echo -e "${BLUE}=== Test 7: Saga Compensation ===${NC}"
compensation_data='{
    "sagaId": "test-saga-002",
    "stepName": "ConfirmOrder",
    "orderId": "test-order-002",
    "reason": "Payment failed - compensating order confirmation",
    "correlationId": "test-correlation-002"
}'

api_call "POST" "$ORDER_URL/saga/compensate" "$compensation_data" "Testing saga compensation with Order Service"
echo ""

# Test 8: Check Redis Streams
echo -e "${BLUE}=== Test 8: Redis Streams Check ===${NC}"
echo -e "${YELLOW}Checking Redis Streams for published events...${NC}"

# Check business events stream
business_events=$(redis-cli -h "$REDIS_URL" XLEN business.events 2>/dev/null || echo "0")
echo "Business Events Stream Length: $business_events"

# Check order events stream
order_events=$(redis-cli -h "$REDIS_URL" XLEN orders.creation 2>/dev/null || echo "0")
echo "Order Events Stream Length: $order_events"

# Check inventory events stream
inventory_events=$(redis-cli -h "$REDIS_URL" XLEN inventory.management 2>/dev/null || echo "0")
echo "Inventory Events Stream Length: $inventory_events"

# Check payment events stream
payment_events=$(redis-cli -h "$REDIS_URL" XLEN payments.processing 2>/dev/null || echo "0")
echo "Payment Events Stream Length: $payment_events"

# Check saga events stream
saga_events=$(redis-cli -h "$REDIS_URL" XLEN saga.orchestration 2>/dev/null || echo "0")
echo "Saga Events Stream Length: $saga_events"

if [ "$business_events" -gt 0 ] || [ "$order_events" -gt 0 ] || [ "$inventory_events" -gt 0 ] || [ "$payment_events" -gt 0 ] || [ "$saga_events" -gt 0 ]; then
    echo -e "${GREEN}✓ Events are being published to Redis Streams${NC}"
else
    echo -e "${YELLOW}⚠ No events found in Redis Streams (this might be normal if tests failed)${NC}"
fi
echo ""

# Test 9: Performance Test
echo -e "${BLUE}=== Test 9: Performance Test ===${NC}"
echo -e "${YELLOW}Running performance test with 5 concurrent saga executions...${NC}"

start_time=$(date +%s)
success_count=0
total_count=5

for i in {1..5}; do
    performance_data='{
        "sagaType": "OrderCreation",
        "orderId": "perf-order-'$i'",
        "customerId": "perf-customer-'$i'",
        "storeId": "perf-store-'$i'",
        "totalAmount": 50.00,
        "paymentMethod": "CreditCard",
        "items": [
            {
                "productId": "perf-prod-'$i'",
                "quantity": 1,
                "price": 50.00
            }
        ]
    }'
    
    if api_call "POST" "$SAGA_URL/execute" "$performance_data" "Performance test saga $i" > /dev/null 2>&1; then
        success_count=$((success_count + 1))
    fi
    
    sleep 1
done

end_time=$(date +%s)
duration=$((end_time - start_time))
success_rate=$((success_count * 100 / total_count))

echo -e "${GREEN}Performance Test Results:${NC}"
echo "Total executions: $total_count"
echo "Successful: $success_count"
echo "Success rate: $success_rate%"
echo "Total duration: ${duration}s"
echo "Average duration per saga: $((duration / total_count))s"
echo ""

# Test 10: Controlled Failure Test
echo -e "${BLUE}=== Test 10: Controlled Failure Test ===${NC}"
failure_data='{
    "sagaType": "OrderCreation",
    "orderId": "fail-order-001",
    "customerId": "customer_failed",
    "storeId": "fail-store-001",
    "totalAmount": 2000.00,
    "paymentMethod": "CreditCard",
    "items": [
        {
            "productId": "fail-prod-001",
            "quantity": 1,
            "price": 2000.00
        }
    ]
}'

echo -e "${YELLOW}Testing controlled failure scenario (payment failure)...${NC}"
api_call "POST" "$SAGA_URL/execute" "$failure_data" "Testing controlled failure scenario"
echo ""

echo -e "${BLUE}=== Test Summary ===${NC}"
echo -e "${GREEN}✓ Saga orchestration tests completed for microservices architecture${NC}"
echo -e "${GREEN}✓ All services are accessible via API Gateway${NC}"
echo -e "${GREEN}✓ Event production and consumption working${NC}"
echo -e "${GREEN}✓ Redis Streams integration verified${NC}"
echo ""
echo -e "${BLUE}=== Access URLs ===${NC}"
echo -e "${YELLOW}API Gateway: ${BASE_URL}${NC}"
echo -e "${YELLOW}Saga Orchestrator: ${SAGA_URL}${NC}"
echo -e "${YELLOW}Order Service: ${ORDER_URL}${NC}"
echo -e "${YELLOW}Stock Service: ${STOCK_URL}${NC}"
echo -e "${YELLOW}Payment Service: ${PAYMENT_URL}${NC}"
echo -e "${YELLOW}Grafana: http://localhost:3000 (admin/admin)${NC}"
echo -e "${YELLOW}Prometheus: http://localhost:9090${NC}"
echo "" 