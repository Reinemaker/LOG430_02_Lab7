#!/bin/bash

# Test script for controlled failures in saga orchestration
# This script demonstrates how controlled failures affect the saga and state machine

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="http://localhost:3000"
API_BASE="$BASE_URL/api"
JWT_TOKEN=""

# Function to authenticate and get JWT token
authenticate() {
    echo "Authenticating with API..."
    local login_data='{"username": "admin", "password": "password"}'
    local auth_response=$(curl -s -X "POST" \
        -H "Content-Type: application/json" \
        -d "$login_data" \
        "$API_BASE/v1/auth/login")
    
    JWT_TOKEN=$(echo "$auth_response" | jq -r '.token // empty')
    if [ -n "$JWT_TOKEN" ]; then
        echo -e "${GREEN}Authentication successful${NC}"
    else
        echo -e "${RED}Authentication failed: $auth_response${NC}"
        exit 1
    fi
}

# Function to make HTTP requests
make_request() {
    local method=$1
    local endpoint=$2
    local data=$3
    
    if [ -n "$data" ]; then
        curl -s -X "$method" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $JWT_TOKEN" \
            -d "$data" \
            "$API_BASE$endpoint"
    else
        curl -s -X "$method" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $JWT_TOKEN" \
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

echo -e "${BLUE}=== CornerShop Controlled Failures Test ===${NC}"
echo "This script demonstrates controlled failures and their effects on saga orchestration"
echo ""

# Authenticate first
authenticate
echo ""

echo -e "${GREEN}Step 1: Check current failure configuration${NC}"
echo "Getting current failure configuration..."
response=$(make_request "GET" "/v1/controlledfailure/config")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 2: Enable controlled failures with high probabilities${NC}"
echo "Setting high failure probabilities to demonstrate effects..."
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

echo -e "${GREEN}Step 3: Test sale saga with controlled failures enabled${NC}"
echo "Creating a sale that will likely trigger controlled failures..."
sale_data='{
    "storeId": "store_001",
    "items": [
        {
            "productName": "Premium Coffee",
            "quantity": 5,
            "unitPrice": 15.99
        },
        {
            "productName": "Organic Milk",
            "quantity": 3,
            "unitPrice": 8.50
        }
    ]
}'

echo "Executing sale saga..."
response=$(make_request "POST" "/v1/saga/sale" "$sale_data")
saga_result=$(echo "$response" | jq -r '.sagaId // empty')
print_json "$response"
echo ""

if [ -n "$saga_result" ]; then
    echo -e "${YELLOW}Saga ID: $saga_result${NC}"
    
    wait_for_user
    
    echo -e "${GREEN}Step 4: Monitor saga state transitions${NC}"
    echo "Checking saga state after potential failures..."
    
    for i in {1..5}; do
        echo "Check $i/5..."
        state_response=$(make_request "GET" "/v1/saga-state/$saga_result")
        current_state=$(echo "$state_response" | jq -r '.currentState // empty')
        error_message=$(echo "$state_response" | jq -r '.errorMessage // empty')
        
        echo "Current State: $current_state"
        if [ "$error_message" != "null" ] && [ -n "$error_message" ]; then
            echo -e "${RED}Error: $error_message${NC}"
        fi
        
        if [ "$current_state" = "Failed" ] || [ "$current_state" = "Compensated" ]; then
            echo -e "${YELLOW}Saga reached final state: $current_state${NC}"
            break
        fi
        
        sleep 2
    done
    
    echo ""
    wait_for_user
    
    echo -e "${GREEN}Step 5: Get detailed saga state and events${NC}"
    state_response=$(make_request "GET" "/v1/saga-state/$saga_result")
    print_json "$state_response"
    echo ""
    
    wait_for_user
    
    echo -e "${GREEN}Step 6: Check compensation results${NC}"
    if [ "$current_state" = "Failed" ] || [ "$current_state" = "Compensated" ]; then
        echo "Getting compensation details from saga result..."
        # Compensation details are included in the saga result
        print_json "$state_response"
    else
        echo "Saga did not fail, no compensation needed"
    fi
    echo ""
    
    wait_for_user
fi

echo -e "${GREEN}Step 7: Test specific failure types${NC}"
echo "Testing individual failure types..."

echo -e "${YELLOW}Testing insufficient stock failure...${NC}"
insufficient_stock_data='{
    "failureType": "insufficientstock",
    "productName": "Premium Coffee",
    "storeId": "store_001",
    "quantity": 100
}'
response=$(make_request "POST" "/v1/controlledfailure/simulate" "$insufficient_stock_data")
print_json "$response"
echo ""

wait_for_user

echo -e "${YELLOW}Testing payment failure...${NC}"
payment_failure_data='{
    "failureType": "paymentfailure",
    "amount": 1500.00,
    "customerId": "customer_001"
}'
response=$(make_request "POST" "/v1/controlledfailure/simulate" "$payment_failure_data")
print_json "$response"
echo ""

wait_for_user

echo -e "${YELLOW}Testing network timeout...${NC}"
network_timeout_data='{
    "failureType": "networktimeout",
    "serviceName": "ProductService"
}'
response=$(make_request "POST" "/v1/controlledfailure/simulate" "$network_timeout_data")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 8: Get affected sagas statistics${NC}"
echo "Getting statistics on sagas affected by failures..."
response=$(make_request "GET" "/v1/controlledfailure/affected-sagas")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 9: Get compensation statistics${NC}"
echo "Getting compensation statistics..."
response=$(make_request "GET" "/v1/controlledfailure/compensation-stats")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 10: Test with different failure probabilities${NC}"
echo "Setting different failure probabilities for testing..."

# Test with low stock probability but high payment failure
config='{
    "InsufficientStockProbability": 0.05,
    "PaymentFailureProbability": 0.4,
    "NetworkTimeoutProbability": 0.02,
    "DatabaseFailureProbability": 0.01,
    "ServiceUnavailableProbability": 0.01
}'
response=$(make_request "PUT" "/v1/controlledfailure/config" "$config")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 11: Test multiple sales with different failure patterns${NC}"
echo "Creating multiple sales to observe different failure patterns..."

for i in {1..3}; do
    echo -e "${YELLOW}Creating sale $i/3...${NC}"
    sale_data='{
        "storeId": "store_002",
        "items": [
            {
                "productName": "Bread",
                "quantity": 2,
                "unitPrice": 3.99
            },
            {
                "productName": "Butter",
                "quantity": 1,
                "unitPrice": 4.50
            }
        ]
    }'
    
    response=$(make_request "POST" "/v1/saga/sale" "$sale_data")
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

echo -e "${GREEN}Step 12: Disable controlled failures and test normal operation${NC}"
echo "Disabling controlled failures..."
response=$(make_request "POST" "/v1/controlledfailure/toggle" "false")
print_json "$response"
echo ""

wait_for_user

echo -e "${YELLOW}Testing normal sale operation...${NC}"
sale_data='{
    "storeId": "store_001",
    "items": [
        {
            "productName": "Coffee",
            "quantity": 2,
            "unitPrice": 12.99
        }
    ]
}'

response=$(make_request "POST" "/v1/saga/sale" "$sale_data")
print_json "$response"
echo ""

wait_for_user

echo -e "${GREEN}Step 13: Final statistics and summary${NC}"
echo "Getting final statistics..."

echo -e "${YELLOW}Compensation Statistics:${NC}"
comp_stats=$(make_request "GET" "/v1/controlledfailure/compensation-stats")
print_json "$comp_stats"
echo ""

echo -e "${YELLOW}Affected Sagas:${NC}"
affected_sagas=$(make_request "GET" "/v1/controlledfailure/affected-sagas")
print_json "$affected_sagas"
echo ""

echo -e "${GREEN}=== Test Summary ===${NC}"
echo "This test demonstrated:"
echo "1. Controlled failure simulation with configurable probabilities"
echo "2. Effects of failures on saga state machine transitions"
echo "3. Compensation mechanisms and rollback actions"
echo "4. Real-time monitoring of saga states and events"
echo "5. Statistics on failure rates and compensation success"
echo "6. Different failure types: insufficient stock, payment failures, network timeouts"
echo ""

echo -e "${BLUE}Test completed successfully!${NC}"
echo "You can now observe how controlled failures affect the saga orchestration"
echo "and how the system handles compensation and rollback actions." 