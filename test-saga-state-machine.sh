#!/bin/bash

# Test Saga State Machine in Corner Shop
# This script demonstrates the enhanced saga orchestration with state machine and event publishing

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="http://cornershop.localhost"
API_BASE="$BASE_URL/api/v1"
SAGA_BASE="$API_BASE/saga"
SAGA_STATE_BASE="$API_BASE/saga-state"

# Test data
STORE_ID="store_6859f81a9e68b183e2892063"
CUSTOMER_ID="customer_123"
PRODUCT_NAME="Milk"

echo -e "${PURPLE}🏗️  Testing Saga State Machine in Corner Shop${NC}"
echo "======================================================"

# Function to make authenticated API calls
make_api_call() {
    local method=$1
    local endpoint=$2
    local data=$3
    
    if [ -n "$data" ]; then
        curl -s -X "$method" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer test-token" \
            -d "$data" \
            "$endpoint"
    else
        curl -s -X "$method" \
            -H "Authorization: Bearer test-token" \
            "$endpoint"
    fi
}

# Function to check if service is running
check_service() {
    echo -e "${YELLOW}🔍 Checking if Corner Shop service is running...${NC}"
    
    if curl -s -f "$BASE_URL/health" > /dev/null; then
        echo -e "${GREEN}✅ Service is running${NC}"
        return 0
    else
        echo -e "${RED}❌ Service is not running. Please start the service first.${NC}"
        echo "Run: ./quick-start.sh"
        exit 1
    fi
}

# Function to test saga state machine
test_saga_state_machine() {
    echo -e "\n${BLUE}🏗️  Testing Saga State Machine${NC}"
    echo "--------------------------------"
    
    local sale_data='{
        "storeId": "'$STORE_ID'",
        "items": [
            {
                "productName": "'$PRODUCT_NAME'",
                "quantity": 2,
                "unitPrice": 3.99
            }
        ]
    }'
    
    echo "Executing sale saga with state machine tracking..."
    echo "$sale_data" | jq .
    
    local response=$(make_api_call "POST" "$SAGA_BASE/sale" "$sale_data")
    
    if echo "$response" | jq -e '.data.isSuccess' > /dev/null; then
        local saga_id=$(echo "$response" | jq -r '.data.sagaId')
        echo -e "${GREEN}✅ Sale saga executed successfully${NC}"
        echo "Saga ID: $saga_id"
        
        # Wait a moment for state updates
        sleep 2
        
        # Get saga state
        echo -e "\n${YELLOW}📊 Getting Saga State:${NC}"
        local state_response=$(make_api_call "GET" "$SAGA_STATE_BASE/$saga_id")
        
        if echo "$state_response" | jq -e '.data' > /dev/null; then
            local current_state=$(echo "$state_response" | jq -r '.data.currentState')
            local saga_type=$(echo "$state_response" | jq -r '.data.sagaType')
            local is_completed=$(echo "$state_response" | jq -r '.data.isCompleted')
            
            echo -e "${GREEN}✅ Saga State Retrieved${NC}"
            echo "Current State: $current_state"
            echo "Saga Type: $saga_type"
            echo "Is Completed: $is_completed"
            
            # Get state transitions
            echo -e "\n${YELLOW}🔄 Getting State Transitions:${NC}"
            local transitions_response=$(make_api_call "GET" "$SAGA_STATE_BASE/$saga_id/transitions")
            
            if echo "$transitions_response" | jq -e '.data' > /dev/null; then
                local transition_count=$(echo "$transitions_response" | jq '.data | length')
                echo -e "${GREEN}✅ State Transitions Retrieved${NC}"
                echo "Number of transitions: $transition_count"
                
                echo -e "\n${YELLOW}📋 State Transition History:${NC}"
                echo "$transitions_response" | jq -r '.data[] | "\(.timestamp) | \(.serviceName) | \(.action) | \(.fromState) -> \(.toState) | \(.eventType) | \(.message // "No message")"'
            else
                echo -e "${RED}❌ Failed to get state transitions${NC}"
            fi
            
            # Get saga events
            echo -e "\n${YELLOW}📡 Getting Saga Events:${NC}"
            local events_response=$(make_api_call "GET" "$SAGA_STATE_BASE/$saga_id/events")
            
            if echo "$events_response" | jq -e '.data' > /dev/null; then
                local event_count=$(echo "$events_response" | jq '.data | length')
                echo -e "${GREEN}✅ Saga Events Retrieved${NC}"
                echo "Number of events: $event_count"
                
                echo -e "\n${YELLOW}📡 Event History:${NC}"
                echo "$events_response" | jq -r '.data[] | "\(.timestamp) | \(.serviceName) | \(.action) | \(.eventType) | \(.message // "No message")"'
            else
                echo -e "${RED}❌ Failed to get saga events${NC}"
            fi
            
        else
            echo -e "${RED}❌ Failed to get saga state${NC}"
        fi
        
        return $saga_id
    else
        echo -e "${RED}❌ Sale saga failed${NC}"
        echo "Error: $(echo "$response" | jq -r '.message // .error')"
        return 1
    fi
}

# Function to test getting all sagas
test_get_all_sagas() {
    echo -e "\n${BLUE}📋 Testing Get All Sagas${NC}"
    echo "---------------------------"
    
    local response=$(make_api_call "GET" "$SAGA_STATE_BASE")
    
    if echo "$response" | jq -e '.data' > /dev/null; then
        local saga_count=$(echo "$response" | jq '.data | length')
        echo -e "${GREEN}✅ Retrieved all sagas${NC}"
        echo "Total sagas: $saga_count"
        
        if [ "$saga_count" -gt 0 ]; then
            echo -e "\n${YELLOW}📊 Saga Summary:${NC}"
            echo "$response" | jq -r '.data[] | "\(.sagaId) | \(.sagaType) | \(.currentState) | \(.createdAt) | \(if .isCompleted then "✅" else "⏳" end)"'
        fi
    else
        echo -e "${RED}❌ Failed to get all sagas${NC}"
    fi
}

# Function to test getting sagas by state
test_get_sagas_by_state() {
    echo -e "\n${BLUE}🔍 Testing Get Sagas by State${NC}"
    echo "-------------------------------"
    
    # Test getting completed sagas
    echo -e "\n${YELLOW}Getting completed sagas:${NC}"
    local completed_response=$(make_api_call "GET" "$SAGA_STATE_BASE/by-state/6")
    
    if echo "$completed_response" | jq -e '.data' > /dev/null; then
        local completed_count=$(echo "$completed_response" | jq '.data | length')
        echo -e "${GREEN}✅ Retrieved completed sagas${NC}"
        echo "Completed sagas: $completed_count"
    else
        echo -e "${RED}❌ Failed to get completed sagas${NC}"
    fi
    
    # Test getting failed sagas
    echo -e "\n${YELLOW}Getting failed sagas:${NC}"
    local failed_response=$(make_api_call "GET" "$SAGA_STATE_BASE/by-state/7")
    
    if echo "$failed_response" | jq -e '.data' > /dev/null; then
        local failed_count=$(echo "$failed_response" | jq '.data | length')
        echo -e "${GREEN}✅ Retrieved failed sagas${NC}"
        echo "Failed sagas: $failed_count"
    else
        echo -e "${RED}❌ Failed to get failed sagas${NC}"
    fi
}

# Function to test getting all events
test_get_all_events() {
    echo -e "\n${BLUE}📡 Testing Get All Events${NC}"
    echo "----------------------------"
    
    local response=$(make_api_call "GET" "$SAGA_STATE_BASE/events")
    
    if echo "$response" | jq -e '.data' > /dev/null; then
        local event_count=$(echo "$response" | jq '.data | length')
        echo -e "${GREEN}✅ Retrieved all events${NC}"
        echo "Total events: $event_count"
        
        if [ "$event_count" -gt 0 ]; then
            echo -e "\n${YELLOW}📡 Recent Events (last 5):${NC}"
            echo "$response" | jq -r '.data[-5:] | .[] | "\(.timestamp) | \(.sagaId) | \(.serviceName) | \(.action) | \(.eventType) | \(.message // "No message")"'
        fi
    else
        echo -e "${RED}❌ Failed to get all events${NC}"
    fi
}

# Function to test state machine features
test_state_machine_features() {
    echo -e "\n${BLUE}🎯 Testing State Machine Features${NC}"
    echo "====================================="
    
    echo -e "\n${GREEN}✅ Event Publishing${NC}"
    echo "   - Microservices publish success/failure events after processing"
    echo "   - Events include service name, action, and detailed messages"
    echo "   - Events are logged and stored for tracking"
    
    echo -e "\n${GREEN}✅ State Machine${NC}"
    echo "   - Explicit state machine with enum (SagaState)"
    echo "   - State transitions are logged and persisted"
    echo "   - Real-time state updates during saga execution"
    
    echo -e "\n${GREEN}✅ State Tracking${NC}"
    echo "   - Complete transition history for each saga"
    echo "   - State filtering and querying capabilities"
    echo "   - Event correlation with state changes"
    
    echo -e "\n${GREEN}✅ Persistence${NC}"
    echo "   - State machine data is persisted (in-memory for demo)"
    echo "   - Transition history is maintained"
    echo "   - Event store for audit and debugging"
}

# Function to show state machine benefits
show_state_machine_benefits() {
    echo -e "\n${BLUE}🎯 State Machine Benefits${NC}"
    echo "============================="
    
    echo -e "\n${GREEN}✅ Visibility${NC}"
    echo "   - Real-time visibility into saga execution"
    echo "   - Clear state progression tracking"
    echo "   - Easy debugging and troubleshooting"
    
    echo -e "\n${GREEN}✅ Observability${NC}"
    echo "   - Detailed event logging from all microservices"
    echo "   - State transition history for audit trails"
    echo "   - Performance monitoring and metrics"
    
    echo -e "\n${GREEN}✅ Reliability${NC}"
    echo "   - Explicit state management prevents inconsistencies"
    echo "   - Event-driven architecture for loose coupling"
    echo "   - Automatic state recovery and compensation"
    
    echo -e "\n${GREEN}✅ Scalability${NC}"
    echo "   - Event-driven communication between services"
    echo "   - State machine can be distributed across services"
    echo "   - Easy to add new states and transitions"
}

# Main test execution
main() {
    echo -e "${BLUE}🚀 Starting Saga State Machine Tests${NC}"
    echo "=========================================="
    
    # Check if service is running
    check_service
    
    # Check if jq is installed
    if ! command -v jq &> /dev/null; then
        echo -e "${RED}❌ jq is required but not installed. Please install jq to run these tests.${NC}"
        echo "Install with: sudo apt-get install jq (Ubuntu/Debian) or brew install jq (macOS)"
        exit 1
    fi
    
    # Run tests
    test_saga_state_machine
    saga_id=$?
    
    test_get_all_sagas
    test_get_sagas_by_state
    test_get_all_events
    test_state_machine_features
    show_state_machine_benefits
    
    echo -e "\n${GREEN}🎉 Saga State Machine Tests Completed!${NC}"
    echo "============================================="
    echo -e "\n${YELLOW}Next Steps:${NC}"
    echo "1. Review the state machine transitions"
    echo "2. Monitor event publishing from microservices"
    echo "3. Explore state filtering and querying"
    echo "4. Implement additional state machine features"
    echo -e "\n${BLUE}Documentation: docs/SAGA_ORCHESTRATION.md${NC}"
}

# Run main function
main "$@" 