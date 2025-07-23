#!/bin/bash

# Test Saga Orchestration in Corner Shop
# This script demonstrates the saga orchestration functionality

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="http://cornershop.localhost"
API_BASE="$BASE_URL/api/v1"
SAGA_BASE="$API_BASE/saga"

# Test data
STORE_ID="store_6859f81a9e68b183e2892063"
CUSTOMER_ID="customer_123"
PRODUCT_NAME="Milk"

echo -e "${BLUE}🧪 Testing Saga Orchestration in Corner Shop${NC}"
echo "=================================================="

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

# Function to test sale saga
test_sale_saga() {
    echo -e "\n${BLUE}📊 Testing Sale Saga${NC}"
    echo "----------------------"
    
    local sale_data='{
        "storeId": "'$STORE_ID'",
        "items": [
            {
                "productName": "'$PRODUCT_NAME'",
                "quantity": 2,
                "unitPrice": 3.99
            },
            {
                "productName": "Bread",
                "quantity": 1,
                "unitPrice": 2.49
            }
        ]
    }'
    
    echo "Executing sale saga with data:"
    echo "$sale_data" | jq .
    
    local response=$(make_api_call "POST" "$SAGA_BASE/sale" "$sale_data")
    
    if echo "$response" | jq -e '.data.isSuccess' > /dev/null; then
        local saga_id=$(echo "$response" | jq -r '.data.sagaId')
        echo -e "${GREEN}✅ Sale saga executed successfully${NC}"
        echo "Saga ID: $saga_id"
        echo "Steps completed: $(echo "$response" | jq '.data.steps | length')"
        
        # Show saga details
        echo -e "\n${YELLOW}Saga Details:${NC}"
        echo "$response" | jq '.data.steps[] | "\(.serviceName) - \(.action): \(if .isCompleted then "✅" else "❌" end)"'
        
        return $saga_id
    else
        echo -e "${RED}❌ Sale saga failed${NC}"
        echo "Error: $(echo "$response" | jq -r '.message // .error')"
        return 1
    fi
}

# Function to test order saga
test_order_saga() {
    echo -e "\n${BLUE}🛒 Testing Order Saga${NC}"
    echo "----------------------"
    
    local order_data='{
        "customerId": "'$CUSTOMER_ID'",
        "storeId": "'$STORE_ID'",
        "items": [
            {
                "productName": "Bread",
                "quantity": 1,
                "unitPrice": 2.49
            }
        ],
        "paymentMethod": "credit_card"
    }'
    
    echo "Executing order saga with data:"
    echo "$order_data" | jq .
    
    local response=$(make_api_call "POST" "$SAGA_BASE/order" "$order_data")
    
    if echo "$response" | jq -e '.data.isSuccess' > /dev/null; then
        local saga_id=$(echo "$response" | jq -r '.data.sagaId')
        echo -e "${GREEN}✅ Order saga executed successfully${NC}"
        echo "Saga ID: $saga_id"
        echo "Steps completed: $(echo "$response" | jq '.data.steps | length')"
        
        # Show saga details
        echo -e "\n${YELLOW}Saga Details:${NC}"
        echo "$response" | jq '.data.steps[] | "\(.serviceName) - \(.action): \(if .isCompleted then "✅" else "❌" end)"'
        
        return $saga_id
    else
        echo -e "${RED}❌ Order saga failed${NC}"
        echo "Error: $(echo "$response" | jq -r '.message // .error')"
        return 1
    fi
}

# Function to test stock update saga
test_stock_saga() {
    echo -e "\n${BLUE}📦 Testing Stock Update Saga${NC}"
    echo "----------------------------"
    
    local stock_data='{
        "productName": "'$PRODUCT_NAME'",
        "storeId": "'$STORE_ID'",
        "quantity": 10,
        "operation": "add"
    }'
    
    echo "Executing stock update saga with data:"
    echo "$stock_data" | jq .
    
    local response=$(make_api_call "POST" "$SAGA_BASE/stock" "$stock_data")
    
    if echo "$response" | jq -e '.data.isSuccess' > /dev/null; then
        local saga_id=$(echo "$response" | jq -r '.data.sagaId')
        echo -e "${GREEN}✅ Stock update saga executed successfully${NC}"
        echo "Saga ID: $saga_id"
        echo "Steps completed: $(echo "$response" | jq '.data.steps | length')"
        
        # Show saga details
        echo -e "\n${YELLOW}Saga Details:${NC}"
        echo "$response" | jq '.data.steps[] | "\(.serviceName) - \(.action): \(if .isCompleted then "✅" else "❌" end)"'
        
        return $saga_id
    else
        echo -e "${RED}❌ Stock update saga failed${NC}"
        echo "Error: $(echo "$response" | jq -r '.message // .error')"
        return 1
    fi
}

# Function to test saga compensation
test_saga_compensation() {
    local saga_id=$1
    echo -e "\n${BLUE}🔄 Testing Saga Compensation${NC}"
    echo "--------------------------------"
    
    if [ -z "$saga_id" ]; then
        echo -e "${YELLOW}⚠️  No saga ID provided for compensation test${NC}"
        return
    fi
    
    echo "Compensating saga: $saga_id"
    
    local response=$(make_api_call "POST" "$SAGA_BASE/compensate/$saga_id")
    
    if echo "$response" | jq -e '.data' > /dev/null; then
        echo -e "${GREEN}✅ Saga compensation executed${NC}"
        echo "Compensation result:"
        echo "$response" | jq '.data'
    else
        echo -e "${RED}❌ Saga compensation failed${NC}"
        echo "Error: $(echo "$response" | jq -r '.message // .error')"
    fi
}

# Function to test invalid saga scenarios
test_invalid_scenarios() {
    echo -e "\n${BLUE}🚫 Testing Invalid Scenarios${NC}"
    echo "-------------------------------"
    
    # Test with invalid store ID
    echo -e "\n${YELLOW}Testing with invalid store ID:${NC}"
    local invalid_sale_data='{
        "storeId": "invalid_store",
        "items": [
            {
                "productName": "Milk",
                "quantity": 1,
                "unitPrice": 3.99
            }
        ]
    }'
    
    local response=$(make_api_call "POST" "$SAGA_BASE/sale" "$invalid_sale_data")
    if echo "$response" | jq -e '.error' > /dev/null; then
        echo -e "${GREEN}✅ Correctly handled invalid store ID${NC}"
        echo "Error: $(echo "$response" | jq -r '.message')"
    else
        echo -e "${RED}❌ Should have failed with invalid store ID${NC}"
    fi
    
    # Test with invalid product
    echo -e "\n${YELLOW}Testing with invalid product:${NC}"
    local invalid_product_data='{
        "storeId": "'$STORE_ID'",
        "items": [
            {
                "productName": "NonExistentProduct",
                "quantity": 1,
                "unitPrice": 3.99
            }
        ]
    }'
    
    response=$(make_api_call "POST" "$SAGA_BASE/sale" "$invalid_product_data")
    if echo "$response" | jq -e '.error' > /dev/null; then
        echo -e "${GREEN}✅ Correctly handled invalid product${NC}"
        echo "Error: $(echo "$response" | jq -r '.message')"
    else
        echo -e "${RED}❌ Should have failed with invalid product${NC}"
    fi
}

# Function to show saga orchestration benefits
show_benefits() {
    echo -e "\n${BLUE}🎯 Saga Orchestration Benefits${NC}"
    echo "================================="
    
    echo -e "\n${GREEN}✅ Data Consistency${NC}"
    echo "   - Ensures inventory and sales data remain consistent"
    echo "   - Prevents overselling products"
    echo "   - Maintains accurate financial records"
    
    echo -e "\n${GREEN}✅ Fault Tolerance${NC}"
    echo "   - Handles network failures gracefully"
    echo "   - Provides automatic rollback mechanisms"
    echo "   - Maintains system stability during partial failures"
    
    echo -e "\n${GREEN}✅ Scalability${NC}"
    echo "   - Supports microservices architecture"
    echo "   - Enables independent service scaling"
    echo "   - Facilitates service deployment and updates"
    
    echo -e "\n${GREEN}✅ Observability${NC}"
    echo "   - Detailed logging of all saga steps"
    echo "   - Clear visibility into transaction flows"
    echo "   - Easy debugging and monitoring"
}

# Main test execution
main() {
    echo -e "${BLUE}🚀 Starting Saga Orchestration Tests${NC}"
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
    test_sale_saga
    sale_saga_id=$?
    
    test_order_saga
    order_saga_id=$?
    
    test_stock_saga
    stock_saga_id=$?
    
    # Test compensation (using the sale saga ID if available)
    if [ "$sale_saga_id" -gt 0 ]; then
        test_saga_compensation "$sale_saga_id"
    fi
    
    # Test invalid scenarios
    test_invalid_scenarios
    
    # Show benefits
    show_benefits
    
    echo -e "\n${GREEN}🎉 Saga Orchestration Tests Completed!${NC}"
    echo "============================================="
    echo -e "\n${YELLOW}Next Steps:${NC}"
    echo "1. Review the saga execution logs"
    echo "2. Monitor saga performance metrics"
    echo "3. Test with different failure scenarios"
    echo "4. Implement additional saga types as needed"
    echo -e "\n${BLUE}Documentation: docs/SAGA_ORCHESTRATION.md${NC}"
}

# Run main function
main "$@" 