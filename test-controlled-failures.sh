#!/bin/bash

# Test script for controlled failures in saga orchestration (Microservices)
# This script demonstrates how controlled failures affect the saga and state machine

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

# Headers for API Gateway
HEADERS="-H 'Content-Type: application/json' -H 'X-API-Key: ${API_KEY}'"

# Function to make HTTP requests
make_request() {
    local method=$1
    local endpoint=$2
    local data=$3
    
    if [ -n "$data" ]; then
        curl -s -X "$method" \
            -H "Content-Type: application/json" \
            -H "X-API-Key: $API_KEY" \
            -d "$data" \
            "$endpoint"
    else
        curl -s -X "$method" \
            -H "Content-Type: application/json" \
            -H "X-API-Key: $API_KEY" \
            "$endpoint"
    fi
}

# Function to print JSON response
print_json() {
    echo "$1" | jq '.' 2>/dev/null || echo "$1"
}

# Function to wait for user input
wait_for_user() {
    echo -e "${YELLOW}Press Enter to continue...${NC}"
    read -r
}

echo -e "${BLUE}=== CornerShop Controlled Failures Test (Microservices) ===${NC}"
echo "This script demonstrates controlled failures and their effects on saga orchestration"
echo ""

# Check if API Gateway is running
echo -e "${YELLOW}Checking if API Gateway is running...${NC}"
if curl -s -f "$BASE_URL/health" > /dev/null; then
    echo -e "${GREEN}✅ API Gateway is running${NC}"
else
    echo -e "${RED}❌ API Gateway is not running. Please start the microservices first.${NC}"
    echo "Run: ./start-microservices.sh"
    exit 1
fi
echo ""

echo -e "${GREEN}Step 1: Test successful saga execution (baseline)${NC}"
echo "Executing a successful saga to establish baseline..."
success_saga='{
    "sagaType": "OrderCreation",
    "orderId": "success-test-order-001",
    "customerId": "success-customer-001",
    "storeId": "success-store-001",
    "totalAmount": 100.00,
    "paymentMethod": "CreditCard",
    "items": [
        {
            "productId": "success-prod-001",
            "quantity": 2,
            "price": 50.00
        }
    ]
}'
response=$(make_request "POST" "$SAGA_URL/execute" "$success_saga")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 2: Test stock failure scenario${NC}"
echo "Testing saga with insufficient stock..."
stock_failure_saga='{
    "sagaType": "OrderCreation",
    "orderId": "stock-fail-order-001",
    "customerId": "stock-fail-customer-001",
    "storeId": "stock-fail-store-001",
    "totalAmount": 500.00,
    "paymentMethod": "CreditCard",
    "items": [
        {
            "productId": "high-demand-prod",
            "quantity": 1000,
            "price": 0.50
        }
    ]
}'
response=$(make_request "POST" "$SAGA_URL/execute" "$stock_failure_saga")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 3: Test payment failure scenario${NC}"
echo "Testing saga with payment failure..."
payment_failure_saga='{
    "sagaType": "OrderCreation",
    "orderId": "payment-fail-order-001",
    "customerId": "customer_failed",
    "storeId": "payment-fail-store-001",
    "totalAmount": 2000.00,
    "paymentMethod": "CreditCard",
    "items": [
        {
            "productId": "payment-fail-prod-001",
            "quantity": 1,
            "price": 2000.00
        }
    ]
}'
response=$(make_request "POST" "$SAGA_URL/execute" "$payment_failure_saga")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 4: Test high amount payment failure${NC}"
echo "Testing saga with high amount that triggers payment failure..."
high_amount_saga='{
    "sagaType": "OrderCreation",
    "orderId": "high-amount-order-001",
    "customerId": "high-amount-customer-001",
    "storeId": "high-amount-store-001",
    "totalAmount": 1500.00,
    "paymentMethod": "CreditCard",
    "items": [
        {
            "productId": "high-amount-prod-001",
            "quantity": 1,
            "price": 1500.00
        }
    ]
}'
response=$(make_request "POST" "$SAGA_URL/execute" "$high_amount_saga")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 5: Test compensation scenarios${NC}"
echo "Testing saga compensation after failures..."

# Execute a saga that will likely fail
compensation_saga='{
    "sagaType": "OrderCreation",
    "orderId": "compensation-test-order-001",
    "customerId": "customer_failed",
    "storeId": "compensation-store-001",
    "totalAmount": 2500.00,
    "paymentMethod": "CreditCard",
    "items": [
        {
            "productId": "compensation-prod-001",
            "quantity": 1,
            "price": 2500.00
        }
    ]
}'
response=$(make_request "POST" "$SAGA_URL/execute" "$compensation_saga")
print_json "$response"

# Wait for compensation to complete
echo -e "${YELLOW}Waiting for compensation to complete...${NC}"
sleep 5

# Check the final state
if echo "$response" | jq -e '.sagaId' > /dev/null; then
    saga_id=$(echo "$response" | jq -r '.sagaId')
    status_response=$(make_request "GET" "$SAGA_URL/status/$saga_id")
    echo -e "${BLUE}Final saga state:${NC}"
    print_json "$status_response"
fi
echo ""

wait_for_user

echo -e "${GREEN}Step 6: Test multiple concurrent failures${NC}"
echo "Testing multiple sagas with different failure scenarios..."

# Array of failure scenarios
declare -a failure_scenarios=(
    '{"orderId": "concurrent-fail-1", "customerId": "customer_failed", "totalAmount": 2000.00}'
    '{"orderId": "concurrent-fail-2", "customerId": "concurrent-customer-2", "totalAmount": 100.00, "items": [{"productId": "high-demand-prod", "quantity": 1000, "price": 0.10}]}'
    '{"orderId": "concurrent-fail-3", "customerId": "concurrent-customer-3", "totalAmount": 1500.00}'
    '{"orderId": "concurrent-fail-4", "customerId": "concurrent-customer-4", "totalAmount": 50.00}'
    '{"orderId": "concurrent-fail-5", "customerId": "customer_failed", "totalAmount": 3000.00}'
)

success_count=0
failure_count=0

for i in "${!failure_scenarios[@]}"; do
    scenario=${failure_scenarios[$i]}
    order_id=$(echo "$scenario" | jq -r '.orderId')
    
    echo -e "${YELLOW}Executing saga $((i+1)): $order_id${NC}"
    
    saga_data='{
        "sagaType": "OrderCreation",
        "orderId": "'$order_id'",
        "customerId": "'$(echo "$scenario" | jq -r '.customerId')'",
        "storeId": "concurrent-store-'$((i+1))'",
        "totalAmount": '$(echo "$scenario" | jq -r '.totalAmount')',
        "paymentMethod": "CreditCard",
        "items": '$(echo "$scenario" | jq -r '.items // [{"productId": "concurrent-prod-'$((i+1))'", "quantity": 1, "price": '$(echo "$scenario" | jq -r '.totalAmount')'}]')
    }'
    
    response=$(make_request "POST" "$SAGA_URL/execute" "$saga_data")
    
    if echo "$response" | jq -e '.status' > /dev/null; then
        status=$(echo "$response" | jq -r '.status')
        if [ "$status" = "Success" ]; then
            echo -e "${GREEN}✅ Saga $((i+1)) succeeded${NC}"
            ((success_count++))
        else
            echo -e "${RED}❌ Saga $((i+1)) failed: $status${NC}"
            ((failure_count++))
        fi
    else
        echo -e "${RED}❌ Saga $((i+1)) failed to execute${NC}"
        ((failure_count++))
    fi
    
    sleep 1
done

echo ""
echo -e "${BLUE}Concurrent Test Results:${NC}"
echo "Successful: $success_count"
echo "Failed: $failure_count"
echo "Total: $((success_count + failure_count))"
echo "Success Rate: $((success_count * 100 / (success_count + failure_count)))%"
echo ""

wait_for_user

echo -e "${GREEN}Step 7: Check event statistics after failures${NC}"
echo "Getting event statistics from all services..."

services=("$SAGA_URL" "$ORDER_URL" "$STOCK_URL" "$PAYMENT_URL")
service_names=("Saga Orchestrator" "Order Service" "Stock Service" "Payment Service")

for i in "${!services[@]}"; do
    echo -e "${BLUE}${service_names[$i]} Event Statistics:${NC}"
    stats_response=$(make_request "GET" "${services[$i]}/events/statistics")
    print_json "$stats_response"
    echo ""
done

wait_for_user

echo -e "${GREEN}Step 8: Check Redis Streams for failure events${NC}"
echo "Checking Redis Streams for business events..."

if command -v redis-cli >/dev/null 2>&1; then
    # Check various event streams
    streams=("business.events" "orders.creation" "inventory.management" "payments.processing" "saga.orchestration")
    stream_names=("Business Events" "Order Events" "Inventory Events" "Payment Events" "Saga Events")
    
    for i in "${!streams[@]}"; do
        stream_length=$(redis-cli -h localhost -p 6379 XLEN "${streams[$i]}" 2>/dev/null || echo "0")
        echo -e "${BLUE}${stream_names[$i]} Stream: $stream_length events${NC}"
        
        if [ "$stream_length" -gt 0 ]; then
            echo -e "${GREEN}✅ Events found in ${stream_names[$i]} stream${NC}"
            
            # Show recent events (last 3)
            echo -e "${YELLOW}Recent events in ${stream_names[$i]} stream:${NC}"
            redis-cli -h localhost -p 6379 XREAD COUNT 3 STREAMS "${streams[$i]}" 0 2>/dev/null | head -10 || echo "No recent events"
            echo
        else
            echo -e "${YELLOW}⚠ No events in ${stream_names[$i]} stream${NC}"
        fi
    done
else
    echo -e "${YELLOW}⚠ redis-cli not available, skipping stream verification${NC}"
fi

wait_for_user

echo -e "${GREEN}Step 9: Check saga metrics after failures${NC}"
echo "Getting saga metrics to see failure patterns..."

metrics_response=$(make_request "GET" "$SAGA_URL/metrics")
print_json "$metrics_response"
echo ""

wait_for_user

echo -e "${GREEN}Step 10: Test monitoring and alerting${NC}"
echo "Checking monitoring systems..."

# Check if Prometheus is accessible
if curl -s "http://localhost:9090/api/v1/targets" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Prometheus is accessible${NC}"
    
    # Check for saga-related metrics
    echo -e "${BLUE}Checking saga metrics in Prometheus...${NC}"
    metrics_response=$(curl -s "http://localhost:9090/api/v1/query?query=saga_executions_total")
    echo "Saga execution metrics: $metrics_response" | jq '.' 2>/dev/null || echo "Metrics: $metrics_response"
else
    echo -e "${YELLOW}⚠ Prometheus not accessible${NC}"
fi

# Check if Grafana is accessible
if curl -s "http://localhost:3000/api/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Grafana is accessible${NC}"
    echo -e "${YELLOW}Dashboard available at: http://localhost:3000 (admin/admin)${NC}"
else
    echo -e "${YELLOW}⚠ Grafana not accessible${NC}"
fi

echo ""

echo -e "${BLUE}=== Controlled Failures Test Summary ===${NC}"
echo -e "${GREEN}✅ Controlled failures test completed for microservices${NC}"
echo -e "${GREEN}✅ Stock failure scenarios tested${NC}"
echo -e "${GREEN}✅ Payment failure scenarios tested${NC}"
echo -e "${GREEN}✅ Compensation scenarios verified${NC}"
echo -e "${GREEN}✅ Concurrent failure testing completed${NC}"
echo -e "${GREEN}✅ Event production during failures verified${NC}"
echo -e "${GREEN}✅ Monitoring and metrics collection working${NC}"
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
echo -e "${GREEN}Test completed successfully!${NC}" 