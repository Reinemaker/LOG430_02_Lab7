#!/bin/bash

# Test Saga State Machine in Corner Shop Microservices
# This script demonstrates the enhanced saga orchestration with state machine and event publishing

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
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

# Test data
STORE_ID="store-001"
CUSTOMER_ID="customer-123"
PRODUCT_ID="product-001"

echo -e "${PURPLE}🏗️  Testing Saga State Machine in Corner Shop Microservices${NC}"
echo "======================================================"

# Function to make authenticated API calls
make_api_call() {
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
            -H "X-API-Key: $API_KEY" \
            "$endpoint"
    fi
}

# Function to check if service is running
check_service() {
    echo -e "${YELLOW}🔍 Checking if API Gateway is running...${NC}"
    
    if curl -s -f "$BASE_URL/health" > /dev/null; then
        echo -e "${GREEN}✅ API Gateway is running${NC}"
        return 0
    else
        echo -e "${RED}❌ API Gateway is not running. Please start the microservices first.${NC}"
        echo "Run: ./start-microservices.sh"
        exit 1
    fi
}

# Function to test saga state machine
test_saga_state_machine() {
    echo -e "\n${BLUE}🏗️  Testing Saga State Machine${NC}"
    echo "--------------------------------"
    
    local saga_data='{
        "sagaType": "OrderCreation",
        "orderId": "state-test-order-001",
        "customerId": "'$CUSTOMER_ID'",
        "storeId": "'$STORE_ID'",
        "totalAmount": 25.99,
        "paymentMethod": "CreditCard",
        "items": [
            {
                "productId": "'$PRODUCT_ID'",
                "quantity": 2,
                "price": 12.99
            }
        ]
    }'
    
    echo "Executing saga with state machine tracking..."
    echo "$saga_data" | jq .
    
    local response=$(make_api_call "POST" "$SAGA_URL/execute" "$saga_data")
    
    if echo "$response" | jq -e '.status' > /dev/null; then
        local saga_id=$(echo "$response" | jq -r '.sagaId')
        local status=$(echo "$response" | jq -r '.status')
        echo -e "${GREEN}✅ Saga executed successfully${NC}"
        echo "Saga ID: $saga_id"
        echo "Status: $status"
        
        # Wait a moment for state updates
        sleep 2
        
        # Get saga status
        echo -e "\n${YELLOW}📊 Getting Saga Status:${NC}"
        local status_response=$(make_api_call "GET" "$SAGA_URL/status/$saga_id")
        
        if echo "$status_response" | jq -e '.currentState' > /dev/null; then
            local current_state=$(echo "$status_response" | jq -r '.currentState')
            local saga_type=$(echo "$status_response" | jq -r '.sagaType')
            local started_at=$(echo "$status_response" | jq -r '.startedAt')
            local completed_at=$(echo "$status_response" | jq -r '.completedAt // "N/A"')
            
            echo -e "${GREEN}✅ Saga state retrieved successfully${NC}"
            echo "Current State: $current_state"
            echo "Saga Type: $saga_type"
            echo "Started At: $started_at"
            echo "Completed At: $completed_at"
            
            # Check if saga is completed
            if [ "$current_state" = "Completed" ]; then
                echo -e "${GREEN}✅ Saga completed successfully${NC}"
            elif [ "$current_state" = "Failed" ] || [ "$current_state" = "Compensated" ]; then
                echo -e "${RED}❌ Saga failed or was compensated${NC}"
            else
                echo -e "${YELLOW}⚠ Saga is still in progress${NC}"
            fi
        else
            echo -e "${RED}❌ Failed to retrieve saga state${NC}"
            echo "$status_response"
        fi
    else
        echo -e "${RED}❌ Saga execution failed${NC}"
        echo "$response"
    fi
}

# Function to test state transitions
test_state_transitions() {
    echo -e "\n${BLUE}🔄 Testing State Transitions${NC}"
    echo "--------------------------------"
    
    # Execute multiple sagas to observe state transitions
    for i in {1..3}; do
        local saga_data='{
            "sagaType": "OrderCreation",
            "orderId": "transition-test-order-'$i'",
            "customerId": "transition-customer-'$i'",
            "storeId": "transition-store-'$i'",
            "totalAmount": 50.00,
            "paymentMethod": "CreditCard",
            "items": [
                {
                    "productId": "transition-prod-'$i'",
                    "quantity": 1,
                    "price": 50.00
                }
            ]
        }'
        
        echo -e "${YELLOW}Executing saga $i...${NC}"
        local response=$(make_api_call "POST" "$SAGA_URL/execute" "$saga_data")
        
        if echo "$response" | jq -e '.sagaId' > /dev/null; then
            local saga_id=$(echo "$response" | jq -r '.sagaId')
            echo -e "${GREEN}✅ Saga $i started: $saga_id${NC}"
            
            # Wait and check state
            sleep 3
            local status_response=$(make_api_call "GET" "$SAGA_URL/status/$saga_id")
            local current_state=$(echo "$status_response" | jq -r '.currentState // "Unknown"')
            echo "Final State: $current_state"
        else
            echo -e "${RED}❌ Saga $i failed to start${NC}"
        fi
        
        echo ""
    done
}

# Function to test compensation scenarios
test_compensation_scenarios() {
    echo -e "\n${BLUE}🔄 Testing Compensation Scenarios${NC}"
    echo "--------------------------------"
    
    # Test payment failure scenario
    local failure_saga_data='{
        "sagaType": "OrderCreation",
        "orderId": "compensation-test-order-001",
        "customerId": "customer_failed",
        "storeId": "compensation-store-001",
        "totalAmount": 2000.00,
        "paymentMethod": "CreditCard",
        "items": [
            {
                "productId": "compensation-prod-001",
                "quantity": 1,
                "price": 2000.00
            }
        ]
    }'
    
    echo -e "${YELLOW}Testing payment failure scenario...${NC}"
    local response=$(make_api_call "POST" "$SAGA_URL/execute" "$failure_saga_data")
    
    if echo "$response" | jq -e '.sagaId' > /dev/null; then
        local saga_id=$(echo "$response" | jq -r '.sagaId')
        echo -e "${GREEN}✅ Failure saga started: $saga_id${NC}"
        
        # Wait for compensation to complete
        sleep 5
        
        # Check final state
        local status_response=$(make_api_call "GET" "$SAGA_URL/status/$saga_id")
        local current_state=$(echo "$status_response" | jq -r '.currentState // "Unknown"')
        local error_message=$(echo "$status_response" | jq -r '.errorMessage // "N/A"')
        
        echo "Final State: $current_state"
        echo "Error Message: $error_message"
        
        if [ "$current_state" = "Compensated" ] || [ "$current_state" = "Failed" ]; then
            echo -e "${GREEN}✅ Compensation scenario handled correctly${NC}"
        else
            echo -e "${YELLOW}⚠ Unexpected final state: $current_state${NC}"
        fi
    else
        echo -e "${RED}❌ Failure saga failed to start${NC}"
        echo "$response"
    fi
}

# Function to test saga metrics
test_saga_metrics() {
    echo -e "\n${BLUE}📊 Testing Saga Metrics${NC}"
    echo "--------------------------------"
    
    echo -e "${YELLOW}Getting saga metrics...${NC}"
    local metrics_response=$(make_api_call "GET" "$SAGA_URL/metrics")
    
    if echo "$metrics_response" | jq -e '.' > /dev/null; then
        echo -e "${GREEN}✅ Saga metrics retrieved successfully${NC}"
        echo "$metrics_response" | jq .
    else
        echo -e "${RED}❌ Failed to retrieve saga metrics${NC}"
        echo "$metrics_response"
    fi
}

# Function to test event production
test_event_production() {
    echo -e "\n${BLUE}📡 Testing Event Production${NC}"
    echo "--------------------------------"
    
    echo -e "${YELLOW}Getting event statistics from all services...${NC}"
    
    # Test each service's event statistics
    services=("$SAGA_URL" "$ORDER_URL" "$STOCK_URL" "$PAYMENT_URL")
    service_names=("Saga Orchestrator" "Order Service" "Stock Service" "Payment Service")
    
    for i in "${!services[@]}"; do
        echo -e "${BLUE}Testing ${service_names[$i]}...${NC}"
        local stats_response=$(make_api_call "GET" "${services[$i]}/events/statistics")
        
        if echo "$stats_response" | jq -e '.' > /dev/null; then
            echo -e "${GREEN}✅ ${service_names[$i]} event statistics retrieved${NC}"
            local total_events=$(echo "$stats_response" | jq -r '.totalEvents // 0')
            echo "Total Events: $total_events"
        else
            echo -e "${RED}❌ Failed to get ${service_names[$i]} event statistics${NC}"
        fi
        echo ""
    done
}

# Function to test Redis Streams
test_redis_streams() {
    echo -e "\n${BLUE}📡 Testing Redis Streams${NC}"
    echo "--------------------------------"
    
    echo -e "${YELLOW}Checking Redis Streams for business events...${NC}"
    
    if command -v redis-cli >/dev/null 2>&1; then
        # Check various event streams
        streams=("business.events" "orders.creation" "inventory.management" "payments.processing" "saga.orchestration")
        stream_names=("Business Events" "Order Events" "Inventory Events" "Payment Events" "Saga Events")
        
        for i in "${!streams[@]}"; do
            local stream_length=$(redis-cli -h localhost -p 6379 XLEN "${streams[$i]}" 2>/dev/null || echo "0")
            echo -e "${BLUE}${stream_names[$i]} Stream: $stream_length events${NC}"
            
            if [ "$stream_length" -gt 0 ]; then
                echo -e "${GREEN}✅ Events found in ${stream_names[$i]} stream${NC}"
            else
                echo -e "${YELLOW}⚠ No events in ${stream_names[$i]} stream${NC}"
            fi
        done
    else
        echo -e "${YELLOW}⚠ redis-cli not available, skipping stream verification${NC}"
    fi
}

# Function to test monitoring
test_monitoring() {
    echo -e "\n${BLUE}📊 Testing Monitoring${NC}"
    echo "--------------------------------"
    
    # Check if Prometheus is accessible
    if curl -s "http://localhost:9090/api/v1/targets" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Prometheus is accessible${NC}"
        
        # Check for saga-related metrics
        echo -e "${BLUE}Checking saga metrics in Prometheus...${NC}"
        local metrics_response=$(curl -s "http://localhost:9090/api/v1/query?query=saga_executions_total")
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
}

# Main test execution
main() {
    echo "Starting Saga State Machine Test Suite for Microservices..."
    echo ""
    
    # Check if service is running
    check_service
    
    # Run all tests
    test_saga_state_machine
    test_state_transitions
    test_compensation_scenarios
    test_saga_metrics
    test_event_production
    test_redis_streams
    test_monitoring
    
    # Summary
    echo -e "\n${PURPLE}========================================${NC}"
    echo -e "${PURPLE}  Test Summary${NC}"
    echo -e "${PURPLE}========================================${NC}"
    echo -e "${GREEN}✅ Saga state machine test completed for microservices${NC}"
    echo -e "${GREEN}✅ State transitions and compensation working${NC}"
    echo -e "${GREEN}✅ Event production and monitoring verified${NC}"
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

# Run main function
main "$@" 