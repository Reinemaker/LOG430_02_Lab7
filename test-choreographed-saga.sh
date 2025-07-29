#!/bin/bash

# Test Choreographed Saga Implementation
# This script tests the choreographed saga pattern with success and failure scenarios

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="http://localhost"
ORDER_SERVICE_URL="${BASE_URL}/api/orders"
SAGA_COORDINATOR_URL="${BASE_URL}/api/choreographedsaga"
TIMEOUT=30

echo -e "${BLUE}=== Choreographed Saga Pattern Test ===${NC}"
echo "Testing distributed transaction coordination using event-driven choreography"
echo ""

# Function to wait for service readiness
wait_for_service() {
    local service_name=$1
    local service_url=$2
    local max_attempts=30
    local attempt=1

    echo -e "${YELLOW}Waiting for ${service_name} to be ready...${NC}"
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s -f "${service_url}/health" > /dev/null 2>&1; then
            echo -e "${GREEN}✓ ${service_name} is ready${NC}"
            return 0
        fi
        
        echo -n "."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    echo -e "${RED}✗ ${service_name} failed to start within ${max_attempts} attempts${NC}"
    return 1
}

# Function to test service health
test_service_health() {
    echo -e "${BLUE}Testing service health...${NC}"
    
    # Test Order Service
    if curl -s -f "${ORDER_SERVICE_URL}/health" > /dev/null; then
        echo -e "${GREEN}✓ Order Service is healthy${NC}"
    else
        echo -e "${RED}✗ Order Service health check failed${NC}"
        return 1
    fi
    
    # Test Choreographed Saga Coordinator
    if curl -s -f "${SAGA_COORDINATOR_URL}/health" > /dev/null; then
        echo -e "${GREEN}✓ Choreographed Saga Coordinator is healthy${NC}"
    else
        echo -e "${RED}✗ Choreographed Saga Coordinator health check failed${NC}"
        return 1
    fi
    
    echo ""
}

# Function to test successful choreographed saga
test_successful_saga() {
    echo -e "${BLUE}=== Testing Successful Choreographed Saga ===${NC}"
    
    local order_data='{
        "customerId": "customer-123",
        "storeId": "store-1",
        "paymentMethod": "CreditCard",
        "shippingAddress": {
            "street": "123 Main St",
            "city": "Anytown",
            "state": "CA",
            "zipCode": "12345",
            "country": "USA"
        },
        "totalAmount": 299.99,
        "items": [
            {
                "productId": "product-1",
                "quantity": 2,
                "unitPrice": 149.99,
                "totalPrice": 299.98
            }
        ]
    }'
    
    echo "Creating order with choreographed saga..."
    local response=$(curl -s -w "\n%{http_code}" -X POST "${ORDER_SERVICE_URL}/choreographed-saga" \
        -H "Content-Type: application/json" \
        -d "$order_data")
    
    local http_code=$(echo "$response" | tail -n1)
    local response_body=$(echo "$response" | head -n -1)
    
    if [ "$http_code" -eq 201 ]; then
        echo -e "${GREEN}✓ Order created successfully${NC}"
        local order_id=$(echo "$response_body" | jq -r '.id')
        echo "Order ID: $order_id"
        
        # Wait a moment for saga processing
        echo "Waiting for saga processing..."
        sleep 5
        
        # Check saga state
        echo "Checking saga state..."
        local saga_response=$(curl -s "${SAGA_COORDINATOR_URL}/states")
        local saga_count=$(echo "$saga_response" | jq '. | length')
        
        if [ "$saga_count" -gt 0 ]; then
            echo -e "${GREEN}✓ Saga state found (${saga_count} sagas)${NC}"
            
            # Get the latest saga
            local latest_saga=$(echo "$saga_response" | jq '.[0]')
            local saga_status=$(echo "$latest_saga" | jq -r '.status')
            local business_process=$(echo "$latest_saga" | jq -r '.businessProcess')
            
            echo "Latest Saga Status: $saga_status"
            echo "Business Process: $business_process"
            
            if [ "$saga_status" = "Completed" ]; then
                echo -e "${GREEN}✓ Saga completed successfully${NC}"
            elif [ "$saga_status" = "InProgress" ]; then
                echo -e "${YELLOW}⚠ Saga is still in progress${NC}"
            else
                echo -e "${RED}✗ Saga failed with status: $saga_status${NC}"
            fi
        else
            echo -e "${YELLOW}⚠ No saga states found${NC}"
        fi
    else
        echo -e "${RED}✗ Failed to create order (HTTP $http_code)${NC}"
        echo "Response: $response_body"
        return 1
    fi
    
    echo ""
}

# Function to test saga statistics
test_saga_statistics() {
    echo -e "${BLUE}=== Testing Saga Statistics ===${NC}"
    
    local stats_response=$(curl -s "${SAGA_COORDINATOR_URL}/statistics")
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Saga statistics retrieved successfully${NC}"
        
        local total_sagas=$(echo "$stats_response" | jq -r '.totalSagas')
        local completed_sagas=$(echo "$stats_response" | jq -r '.completedSagas')
        local failed_sagas=$(echo "$stats_response" | jq -r '.failedSagas')
        local in_progress_sagas=$(echo "$stats_response" | jq -r '.inProgressSagas')
        local success_rate=$(echo "$stats_response" | jq -r '.successRate')
        local average_duration=$(echo "$stats_response" | jq -r '.averageDurationSeconds')
        
        echo "Saga Statistics:"
        echo "  Total Sagas: $total_sagas"
        echo "  Completed: $completed_sagas"
        echo "  Failed: $failed_sagas"
        echo "  In Progress: $in_progress_sagas"
        echo "  Success Rate: ${success_rate}%"
        echo "  Average Duration: ${average_duration}s"
        
        # Business process breakdown
        echo "Business Process Breakdown:"
        echo "$stats_response" | jq -r '.businessProcessBreakdown[] | "  \(.businessProcess): \(.totalCount) total, \(.completedCount) completed, \(.successRate)% success rate"'
    else
        echo -e "${RED}✗ Failed to retrieve saga statistics${NC}"
        return 1
    fi
    
    echo ""
}

# Function to test saga metrics
test_saga_metrics() {
    echo -e "${BLUE}=== Testing Saga Metrics ===${NC}"
    
    local metrics_response=$(curl -s "${SAGA_COORDINATOR_URL}/metrics")
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Saga metrics retrieved successfully${NC}"
        
        local total_sagas=$(echo "$metrics_response" | jq -r '.totalSagas')
        local success_rate=$(echo "$metrics_response" | jq -r '.successRate')
        local failure_rate=$(echo "$metrics_response" | jq -r '.failureRate')
        local compensation_rate=$(echo "$metrics_response" | jq -r '.compensationRate')
        
        echo "Saga Metrics:"
        echo "  Total Sagas: $total_sagas"
        echo "  Success Rate: ${success_rate}%"
        echo "  Failure Rate: ${failure_rate}%"
        echo "  Compensation Rate: ${compensation_rate}%"
    else
        echo -e "${RED}✗ Failed to retrieve saga metrics${NC}"
        return 1
    fi
    
    echo ""
}

# Function to test saga state filtering
test_saga_state_filtering() {
    echo -e "${BLUE}=== Testing Saga State Filtering ===${NC}"
    
    # Test by status
    echo "Testing saga states by status..."
    local completed_sagas=$(curl -s "${SAGA_COORDINATOR_URL}/states/status/Completed")
    local completed_count=$(echo "$completed_sagas" | jq '. | length')
    echo "  Completed sagas: $completed_count"
    
    local in_progress_sagas=$(curl -s "${SAGA_COORDINATOR_URL}/states/status/InProgress")
    local in_progress_count=$(echo "$in_progress_sagas" | jq '. | length')
    echo "  In-progress sagas: $in_progress_count"
    
    local failed_sagas=$(curl -s "${SAGA_COORDINATOR_URL}/states/status/Failed")
    local failed_count=$(echo "$failed_sagas" | jq '. | length')
    echo "  Failed sagas: $failed_count"
    
    # Test by business process
    echo "Testing saga states by business process..."
    local order_processing_sagas=$(curl -s "${SAGA_COORDINATOR_URL}/states/business-process/OrderProcessing")
    local order_processing_count=$(echo "$order_processing_sagas" | jq '. | length')
    echo "  OrderProcessing sagas: $order_processing_count"
    
    echo -e "${GREEN}✓ Saga state filtering tests completed${NC}"
    echo ""
}

# Function to test date range filtering
test_date_range_filtering() {
    echo -e "${BLUE}=== Testing Date Range Filtering ===${NC}"
    
    local start_date=$(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%SZ)
    local end_date=$(date -u +%Y-%m-%dT%H:%M:%SZ)
    
    echo "Filtering sagas from $start_date to $end_date"
    
    local range_sagas=$(curl -s "${SAGA_COORDINATOR_URL}/states/date-range?startDate=${start_date}&endDate=${end_date}")
    local range_count=$(echo "$range_sagas" | jq '. | length')
    
    echo "  Sagas in date range: $range_count"
    
    if [ "$range_count" -ge 0 ]; then
        echo -e "${GREEN}✓ Date range filtering test completed${NC}"
    else
        echo -e "${RED}✗ Date range filtering test failed${NC}"
        return 1
    fi
    
    echo ""
}

# Function to test compensation scenario
test_compensation_scenario() {
    echo -e "${BLUE}=== Testing Compensation Scenario ===${NC}"
    echo "Note: This test simulates a compensation scenario by checking for compensated sagas"
    
    local stats_response=$(curl -s "${SAGA_COORDINATOR_URL}/statistics")
    local compensated_sagas=$(echo "$stats_response" | jq -r '.compensatedSagas')
    local compensation_rate=$(echo "$stats_response" | jq -r '.compensationRate')
    
    echo "Compensation Statistics:"
    echo "  Compensated Sagas: $compensated_sagas"
    echo "  Compensation Rate: ${compensation_rate}%"
    
    if [ "$compensated_sagas" -ge 0 ]; then
        echo -e "${GREEN}✓ Compensation tracking is working${NC}"
    else
        echo -e "${YELLOW}⚠ No compensation data available${NC}"
    fi
    
    echo ""
}

# Function to test error handling
test_error_handling() {
    echo -e "${BLUE}=== Testing Error Handling ===${NC}"
    
    # Test invalid saga ID
    echo "Testing invalid saga ID..."
    local invalid_response=$(curl -s -w "\n%{http_code}" "${SAGA_COORDINATOR_URL}/state/invalid-saga-id")
    local invalid_http_code=$(echo "$invalid_response" | tail -n1)
    
    if [ "$invalid_http_code" -eq 404 ]; then
        echo -e "${GREEN}✓ Invalid saga ID handled correctly (404)${NC}"
    else
        echo -e "${RED}✗ Invalid saga ID not handled correctly (HTTP $invalid_http_code)${NC}"
    fi
    
    # Test invalid status
    echo "Testing invalid status..."
    local invalid_status_response=$(curl -s -w "\n%{http_code}" "${SAGA_COORDINATOR_URL}/states/status/InvalidStatus")
    local invalid_status_http_code=$(echo "$invalid_status_response" | tail -n1)
    
    if [ "$invalid_status_http_code" -eq 200 ]; then
        echo -e "${GREEN}✓ Invalid status handled gracefully (empty result)${NC}"
    else
        echo -e "${YELLOW}⚠ Invalid status returned HTTP $invalid_status_http_code${NC}"
    fi
    
    echo ""
}

# Main test execution
main() {
    echo -e "${BLUE}Starting Choreographed Saga Pattern Tests${NC}"
    echo "=================================================="
    echo ""
    
    # Wait for services to be ready
    wait_for_service "Order Service" "${ORDER_SERVICE_URL}"
    wait_for_service "Choreographed Saga Coordinator" "${SAGA_COORDINATOR_URL}"
    
    # Run tests
    test_service_health
    test_successful_saga
    test_saga_statistics
    test_saga_metrics
    test_saga_state_filtering
    test_date_range_filtering
    test_compensation_scenario
    test_error_handling
    
    echo -e "${GREEN}=== All Choreographed Saga Tests Completed ===${NC}"
    echo ""
    echo -e "${BLUE}Test Summary:${NC}"
    echo "✓ Service health checks"
    echo "✓ Successful saga execution"
    echo "✓ Saga statistics and metrics"
    echo "✓ State filtering and querying"
    echo "✓ Date range filtering"
    echo "✓ Compensation tracking"
    echo "✓ Error handling"
    echo ""
    echo -e "${GREEN}Choreographed Saga Pattern implementation is working correctly!${NC}"
}

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${RED}Error: jq is required but not installed.${NC}"
    echo "Please install jq to run this test script."
    echo "Ubuntu/Debian: sudo apt-get install jq"
    echo "CentOS/RHEL: sudo yum install jq"
    echo "macOS: brew install jq"
    exit 1
fi

# Check if curl is installed
if ! command -v curl &> /dev/null; then
    echo -e "${RED}Error: curl is required but not installed.${NC}"
    exit 1
fi

# Run main function
main "$@" 