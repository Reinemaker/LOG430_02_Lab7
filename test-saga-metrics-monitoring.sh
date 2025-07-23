#!/bin/bash

# Test script for saga metrics, monitoring, and structured logging
# This script demonstrates Prometheus metrics, Grafana visualization, and structured business event logging

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="http://localhost:5000"
API_BASE="$BASE_URL/api"

echo -e "${BLUE}=== CornerShop Saga Metrics & Monitoring Test ===${NC}"
echo "This script demonstrates Prometheus metrics, Grafana visualization, and structured logging"
echo ""

# Function to make HTTP requests
make_request() {
    local method=$1
    local endpoint=$2
    local data=$3
    
    if [ -n "$data" ]; then
        curl -s -X "$method" \
            -H "Content-Type: application/json" \
            -d "$data" \
            "$API_BASE$endpoint"
    else
        curl -s -X "$method" \
            -H "Content-Type: application/json" \
            "$API_BASE$endpoint"
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

echo -e "${GREEN}Step 1: Check initial metrics summary${NC}"
echo "Getting initial metrics summary..."
response=$(make_request "GET" "/SagaMetrics/summary")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 2: Execute multiple sagas to generate metrics${NC}"
echo "Creating multiple sales to generate comprehensive metrics..."

for i in {1..5}; do
    echo -e "${YELLOW}Creating sale $i/5...${NC}"
    sale_data='{
        "storeId": "store_001",
        "items": [
            {
                "productName": "Coffee",
                "quantity": 2,
                "unitPrice": 12.99
            },
            {
                "productName": "Bread",
                "quantity": 1,
                "unitPrice": 3.99
            }
        ]
    }'
    
    response=$(make_request "POST" "/SagaOrchestrator/sale" "$sale_data")
    saga_id=$(echo "$response" | jq -r '.sagaId // empty')
    success=$(echo "$response" | jq -r '.isSuccess // false')
    
    echo "Saga ID: $saga_id, Success: $success"
    
    if [ "$success" = "false" ]; then
        error=$(echo "$response" | jq -r '.errorMessage // empty')
        echo -e "${RED}Error: $error${NC}"
    fi
    
    sleep 1
done

echo ""
wait_for_user

echo -e "${GREEN}Step 3: Enable controlled failures and test with metrics${NC}"
echo "Enabling controlled failures to observe failure metrics..."

config='{
    "EnableFailures": true,
    "InsufficientStockProbability": 0.3,
    "PaymentFailureProbability": 0.2,
    "NetworkTimeoutProbability": 0.15,
    "DatabaseFailureProbability": 0.1,
    "ServiceUnavailableProbability": 0.05,
    "FailureDelayMs": 500
}'
response=$(make_request "PUT" "/ControlledFailure/config" "$config")
print_json "$response"
echo ""

wait_for_user

echo -e "${YELLOW}Creating sales with controlled failures...${NC}"
for i in {1..3}; do
    echo -e "${YELLOW}Creating sale with failures $i/3...${NC}"
    sale_data='{
        "storeId": "store_001",
        "items": [
            {
                "productName": "Premium Coffee",
                "quantity": 5,
                "unitPrice": 15.99
            }
        ]
    }'
    
    response=$(make_request "POST" "/SagaOrchestrator/sale" "$sale_data")
    saga_id=$(echo "$response" | jq -r '.sagaId // empty')
    success=$(echo "$response" | jq -r '.isSuccess // false')
    
    echo "Saga ID: $saga_id, Success: $success"
    
    if [ "$success" = "false" ]; then
        error=$(echo "$response" | jq -r '.errorMessage // empty')
        echo -e "${RED}Error: $error${NC}"
    fi
    
    sleep 2
done

echo ""
wait_for_user

echo -e "${GREEN}Step 4: Check updated metrics summary${NC}"
echo "Getting updated metrics after saga executions..."
response=$(make_request "GET" "/SagaMetrics/summary")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 5: Get saga performance statistics${NC}"
echo "Getting detailed performance statistics..."
response=$(make_request "GET" "/SagaMetrics/performance")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 6: Get saga state distribution${NC}"
echo "Getting current state distribution..."
response=$(make_request "GET" "/SagaMetrics/state-distribution")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 7: Get saga transition analysis${NC}"
echo "Getting transition analysis..."
response=$(make_request "GET" "/SagaMetrics/transition-analysis")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 8: Get saga duration statistics${NC}"
echo "Getting duration statistics..."
response=$(make_request "GET" "/SagaMetrics/duration-stats")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 9: Get recent saga activity${NC}"
echo "Getting recent saga activity..."
response=$(make_request "GET" "/SagaMetrics/recent-activity?hours=1")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 10: Get Prometheus metrics format${NC}"
echo "Getting Prometheus metrics in text format..."
response=$(make_request "GET" "/SagaMetrics/prometheus")
echo "Prometheus metrics (first 20 lines):"
echo "$response" | head -20
echo "..."
echo ""

wait_for_user

echo -e "${GREEN}Step 11: Get Grafana integration information${NC}"
echo "Getting Grafana integration details..."
response=$(make_request "GET" "/SagaMetrics/grafana")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 12: Test structured logging with business events${NC}"
echo "Testing business event logging..."

# Test controlled failure simulation
failure_data='{
    "failureType": "insufficientstock",
    "productName": "Premium Coffee",
    "storeId": "store_001",
    "quantity": 100
}'
response=$(make_request "POST" "/ControlledFailure/simulate" "$failure_data")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 13: Check compensation statistics${NC}"
echo "Getting compensation statistics..."
response=$(make_request "GET" "/ControlledFailure/compensation-stats")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 14: Get affected sagas${NC}"
echo "Getting sagas affected by failures..."
response=$(make_request "GET" "/ControlledFailure/affected-sagas")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 15: Disable controlled failures and test normal operation${NC}"
echo "Disabling controlled failures..."
response=$(make_request "POST" "/ControlledFailure/toggle" "false")
print_json "$response"
echo ""

wait_for_user

echo -e "${YELLOW}Creating normal sales to observe success metrics...${NC}"
for i in {1..3}; do
    echo -e "${YELLOW}Creating normal sale $i/3...${NC}"
    sale_data='{
        "storeId": "store_002",
        "items": [
            {
                "productName": "Milk",
                "quantity": 2,
                "unitPrice": 4.50
            }
        ]
    }'
    
    response=$(make_request "POST" "/SagaOrchestrator/sale" "$sale_data")
    saga_id=$(echo "$response" | jq -r '.sagaId // empty')
    success=$(echo "$response" | jq -r '.isSuccess // false')
    
    echo "Saga ID: $saga_id, Success: $success"
    
    sleep 1
done

echo ""
wait_for_user

echo -e "${GREEN}Step 16: Final metrics summary${NC}"
echo "Getting final comprehensive metrics summary..."

echo -e "${YELLOW}Performance Statistics:${NC}"
performance=$(make_request "GET" "/SagaMetrics/performance")
print_json "$performance"
echo ""

echo -e "${YELLOW}State Distribution:${NC}"
state_dist=$(make_request "GET" "/SagaMetrics/state-distribution")
print_json "$state_dist"
echo ""

echo -e "${YELLOW}Duration Statistics:${NC}"
duration_stats=$(make_request "GET" "/SagaMetrics/duration-stats")
print_json "$duration_stats"
echo ""

wait_for_user

echo -e "${GREEN}Step 17: Grafana Dashboard Information${NC}"
echo "Grafana Dashboard Configuration:"
echo ""
echo -e "${BLUE}Dashboard URL:${NC} http://localhost:3000"
echo -e "${BLUE}Dashboard ID:${NC} saga-monitoring"
echo -e "${BLUE}Dashboard Title:${NC} Saga Orchestration Monitoring"
echo ""
echo -e "${BLUE}Key Metrics Available:${NC}"
echo "• saga_total - Total number of sagas by type"
echo "• saga_success_total - Successful sagas"
echo "• saga_failure_total - Failed sagas"
echo "• saga_duration_seconds - Saga execution duration"
echo "• saga_step_total - Total saga steps"
echo "• saga_step_success_total - Successful saga steps"
echo "• saga_step_failure_total - Failed saga steps"
echo "• saga_step_duration_seconds - Step execution duration"
echo "• saga_state_transition_total - State transitions"
echo "• saga_compensation_total - Compensation actions"
echo "• controlled_failure_total - Controlled failures"
echo "• business_event_total - Business events"
echo "• saga_active - Currently active sagas"
echo "• saga_by_state - Sagas by current state"
echo ""

wait_for_user

echo -e "${GREEN}Step 18: Structured Logging Information${NC}"
echo "Structured logging features implemented:"
echo ""
echo -e "${BLUE}Business Events:${NC}"
echo "• step_completed - Saga step completion"
echo "• step_failed - Saga step failure"
echo "• lifecycle_started - Saga lifecycle start"
echo "• lifecycle_completed - Saga lifecycle completion"
echo "• lifecycle_failed - Saga lifecycle failure"
echo "• state_transition - State machine transitions"
echo "• compensation - Compensation actions"
echo "• controlled_failure - Controlled failure events"
echo ""
echo -e "${BLUE}Log Format:${NC}"
echo "• JSON structured format"
echo "• Correlation IDs for tracing"
echo "• Timestamp and environment information"
echo "• Detailed context data"
echo "• Severity levels"
echo ""

wait_for_user

echo -e "${GREEN}=== Test Summary ===${NC}"
echo "This test demonstrated:"
echo "1. Prometheus metrics collection for saga orchestration"
echo "2. Real-time metrics tracking (duration, success rates, failures)"
echo "3. State transition monitoring and analysis"
echo "4. Compensation tracking and statistics"
echo "5. Controlled failure metrics"
echo "6. Business event structured logging"
echo "7. Grafana dashboard integration"
echo "8. Performance analysis and reporting"
echo "9. Recent activity monitoring"
echo "10. Comprehensive metrics API endpoints"
echo ""

echo -e "${BLUE}Next Steps:${NC}"
echo "1. Access Grafana at http://localhost:3000"
echo "2. Import the saga-monitoring dashboard"
echo "3. Configure Prometheus data source"
echo "4. Monitor real-time saga orchestration"
echo "5. Analyze structured logs for business insights"
echo ""

echo -e "${BLUE}Test completed successfully!${NC}"
echo "You can now monitor saga orchestration with comprehensive metrics"
echo "and visualize state evolution through Grafana dashboards." 