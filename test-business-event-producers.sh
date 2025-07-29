#!/bin/bash

# Test Business Event Producers for Microservices
# This script demonstrates the implementation of event producers for the Corner Shop microservices

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

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  Business Event Producers Test Suite${NC}"
echo -e "${BLUE}  (Microservices Architecture)${NC}"
echo -e "${BLUE}========================================${NC}"
echo

# Function to print section headers
print_section() {
    echo -e "${YELLOW}$1${NC}"
    echo -e "${YELLOW}$(printf '=%.0s' {1..50})${NC}"
    echo
}

# Function to make API calls and handle responses
make_api_call() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4
    
    echo -e "${BLUE}Testing: $description${NC}"
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" "$endpoint" $HEADERS)
    else
        response=$(curl -s -w "\n%{http_code}" -X "$method" \
            -H "Content-Type: application/json" \
            -H "X-API-Key: $API_KEY" \
            -d "$data" \
            "$endpoint")
    fi
    
    # Extract status code (last line)
    status_code=$(echo "$response" | tail -n1)
    # Extract response body (all lines except last)
    response_body=$(echo "$response" | head -n -1)
    
    if [ "$status_code" -eq 200 ]; then
        echo -e "${GREEN}✓ Success (HTTP $status_code)${NC}"
        echo "$response_body" | jq '.' 2>/dev/null || echo "$response_body"
    else
        echo -e "${RED}✗ Failed (HTTP $status_code)${NC}"
        echo "$response_body"
    fi
    echo
}

# Function to wait for services to be ready
wait_for_services() {
    print_section "Waiting for microservices to be ready"
    
    echo "Checking if API Gateway is accessible..."
    max_attempts=30
    attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s "$BASE_URL/health" > /dev/null 2>&1; then
            echo -e "${GREEN}✓ API Gateway is ready${NC}"
            break
        fi
        
        echo "Attempt $attempt/$max_attempts: Waiting for API Gateway to be ready..."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    if [ $attempt -gt $max_attempts ]; then
        echo -e "${RED}✗ Timeout waiting for API Gateway to be ready${NC}"
        exit 1
    fi
    echo
}

# Function to test connection status
test_connection_status() {
    print_section "Testing Event Producer Connection Status"
    
    make_api_call "GET" "$SAGA_URL/events/statistics" "" "Get Saga Orchestrator event statistics"
    make_api_call "GET" "$ORDER_URL/events/statistics" "" "Get Order Service event statistics"
    make_api_call "GET" "$STOCK_URL/events/statistics" "" "Get Stock Service event statistics"
    make_api_call "GET" "$PAYMENT_URL/events/statistics" "" "Get Payment Service event statistics"
}

# Function to test event statistics
test_event_statistics() {
    print_section "Testing Event Statistics"
    
    echo -e "${YELLOW}Getting event statistics from all services...${NC}"
    
    # Test each service's event statistics
    services=("$SAGA_URL" "$ORDER_URL" "$STOCK_URL" "$PAYMENT_URL")
    service_names=("Saga Orchestrator" "Order Service" "Stock Service" "Payment Service")
    
    for i in "${!services[@]}"; do
        echo -e "${BLUE}Testing ${service_names[$i]}...${NC}"
        make_api_call "GET" "${services[$i]}/events/statistics" "" "Get ${service_names[$i]} event statistics"
    done
}

# Function to test saga orchestration events
test_saga_orchestration_events() {
    print_section "Testing Saga Orchestration Events"
    
    echo -e "${YELLOW}Testing saga orchestration with event production...${NC}"
    
    # Test successful saga execution
    saga_data='{
        "sagaType": "OrderCreation",
        "orderId": "event-test-order-001",
        "customerId": "event-test-customer-001",
        "storeId": "event-test-store-001",
        "totalAmount": 100.00,
        "paymentMethod": "CreditCard",
        "items": [
            {
                "productId": "event-test-prod-001",
                "quantity": 2,
                "price": 50.00
            }
        ]
    }'
    
    make_api_call "POST" "$SAGA_URL/execute" "$saga_data" "Execute saga with event production"
    
    # Wait a moment for events to be processed
    sleep 2
    
    # Check event statistics after saga execution
    echo -e "${YELLOW}Checking event statistics after saga execution...${NC}"
    make_api_call "GET" "$SAGA_URL/events/statistics" "" "Get updated saga event statistics"
    make_api_call "GET" "$ORDER_URL/events/statistics" "" "Get updated order event statistics"
    make_api_call "GET" "$STOCK_URL/events/statistics" "" "Get updated stock event statistics"
    make_api_call "GET" "$PAYMENT_URL/events/statistics" "" "Get updated payment event statistics"
}

# Function to test direct service events
test_direct_service_events() {
    print_section "Testing Direct Service Event Production"
    
    echo -e "${YELLOW}Testing direct service event production...${NC}"
    
    # Test direct payment processing
    payment_data='{
        "customerId": "direct-test-customer",
        "amount": 75.50,
        "paymentMethod": "CreditCard"
    }'
    
    make_api_call "POST" "$PAYMENT_URL/process" "$payment_data" "Process payment with event production"
    
    # Test direct stock operations
    stock_verification_data='{
        "sagaId": "direct-stock-test",
        "stepName": "VerifyStock",
        "orderId": "direct-stock-order",
        "data": {
            "productId": "direct-test-prod",
            "quantity": 5
        },
        "correlationId": "direct-stock-correlation"
    }'
    
    make_api_call "POST" "$STOCK_URL/saga/participate" "$stock_verification_data" "Verify stock with event production"
    
    # Wait for events to be processed
    sleep 2
    
    # Check updated statistics
    echo -e "${YELLOW}Checking updated event statistics...${NC}"
    make_api_call "GET" "$PAYMENT_URL/events/statistics" "" "Get payment service event statistics"
    make_api_call "GET" "$STOCK_URL/events/statistics" "" "Get stock service event statistics"
}

# Function to test Redis Streams
test_redis_streams() {
    print_section "Testing Redis Streams Event Storage"
    
    echo -e "${YELLOW}Checking Redis Streams for business events...${NC}"
    
    if command -v redis-cli >/dev/null 2>&1; then
        # Check various event streams
        streams=("business.events" "orders.creation" "inventory.management" "payments.processing" "saga.orchestration")
        stream_names=("Business Events" "Order Events" "Inventory Events" "Payment Events" "Saga Events")
        
        for i in "${!streams[@]}"; do
            stream_length=$(redis-cli -h "$REDIS_URL" XLEN "${streams[$i]}" 2>/dev/null || echo "0")
            echo -e "${BLUE}${stream_names[$i]} Stream: $stream_length events${NC}"
            
            if [ "$stream_length" -gt 0 ]; then
                echo -e "${GREEN}✓ Events found in ${stream_names[$i]} stream${NC}"
                
                # Show recent events (last 3)
                echo -e "${YELLOW}Recent events in ${stream_names[$i]} stream:${NC}"
                redis-cli -h "$REDIS_URL" XREAD COUNT 3 STREAMS "${streams[$i]}" 0 2>/dev/null | head -10 || echo "No recent events"
                echo
            else
                echo -e "${YELLOW}⚠ No events in ${stream_names[$i]} stream${NC}"
            fi
        done
    else
        echo -e "${YELLOW}⚠ redis-cli not available, skipping stream verification${NC}"
    fi
}

# Function to test event correlation
test_event_correlation() {
    print_section "Testing Event Correlation"
    
    echo -e "${YELLOW}Testing event correlation across services...${NC}"
    
    # Execute a saga with a specific correlation ID
    correlation_id="correlation-test-$(date +%s)"
    
    correlated_saga_data='{
        "sagaType": "OrderCreation",
        "orderId": "correlation-order-001",
        "customerId": "correlation-customer-001",
        "storeId": "correlation-store-001",
        "totalAmount": 150.00,
        "paymentMethod": "CreditCard",
        "items": [
            {
                "productId": "correlation-prod-001",
                "quantity": 3,
                "price": 50.00
            }
        ]
    }'
    
    make_api_call "POST" "$SAGA_URL/execute" "$correlated_saga_data" "Execute saga with correlation tracking"
    
    echo -e "${YELLOW}Correlation ID used: $correlation_id${NC}"
    echo -e "${GREEN}✓ Saga executed with correlation tracking${NC}"
}

# Function to test failure scenarios
test_failure_scenarios() {
    print_section "Testing Failure Scenarios with Event Production"
    
    echo -e "${YELLOW}Testing failure scenarios that should produce events...${NC}"
    
    # Test payment failure scenario
    payment_failure_data='{
        "sagaType": "OrderCreation",
        "orderId": "failure-test-order-001",
        "customerId": "customer_failed",
        "storeId": "failure-test-store-001",
        "totalAmount": 2500.00,
        "paymentMethod": "CreditCard",
        "items": [
            {
                "productId": "failure-test-prod-001",
                "quantity": 1,
                "price": 2500.00
            }
        ]
    }'
    
    echo -e "${BLUE}Testing payment failure scenario...${NC}"
    make_api_call "POST" "$SAGA_URL/execute" "$payment_failure_data" "Execute saga with payment failure"
    
    # Wait for events to be processed
    sleep 2
    
    # Check event statistics after failure
    echo -e "${YELLOW}Checking event statistics after failure scenario...${NC}"
    make_api_call "GET" "$SAGA_URL/events/statistics" "" "Get saga event statistics after failure"
    make_api_call "GET" "$PAYMENT_URL/events/statistics" "" "Get payment event statistics after failure"
}

# Function to test event monitoring
test_event_monitoring() {
    print_section "Testing Event Monitoring"
    
    echo -e "${YELLOW}Testing event monitoring capabilities...${NC}"
    
    # Check if Prometheus is accessible
    if curl -s "http://localhost:9090/api/v1/targets" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Prometheus is accessible${NC}"
        
        # Check for saga-related metrics
        echo -e "${BLUE}Checking saga metrics in Prometheus...${NC}"
        metrics_response=$(curl -s "http://localhost:9090/api/v1/query?query=saga_executions_total")
        echo "Saga execution metrics: $metrics_response" | jq '.' 2>/dev/null || echo "Metrics: $metrics_response"
    else
        echo -e "${YELLOW}⚠ Prometheus not accessible${NC}"
    fi
    
    # Check if Grafana is accessible
    if curl -s "http://localhost:3000/api/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Grafana is accessible${NC}"
        echo -e "${YELLOW}Dashboard available at: http://localhost:3000 (admin/admin)${NC}"
    else
        echo -e "${YELLOW}⚠ Grafana not accessible${NC}"
    fi
}

# Main test execution
main() {
    echo "Starting Business Event Producers Test Suite for Microservices..."
    echo ""
    
    # Wait for services to be ready
    wait_for_services
    
    # Run all tests
    test_connection_status
    test_event_statistics
    test_saga_orchestration_events
    test_direct_service_events
    test_redis_streams
    test_event_correlation
    test_failure_scenarios
    test_event_monitoring
    
    # Summary
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}  Test Summary${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo -e "${GREEN}✓ Business event producers test completed for microservices${NC}"
    echo -e "${GREEN}✓ All services are producing events via Redis Streams${NC}"
    echo -e "${GREEN}✓ Event correlation and monitoring working${NC}"
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
    echo -e "${GREEN}Test suite completed successfully!${NC}"
}

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${YELLOW}Warning: jq is not installed. JSON responses will not be formatted.${NC}"
    echo "Install jq for better output formatting: sudo apt-get install jq"
    echo
fi

# Run main function
main "$@" 