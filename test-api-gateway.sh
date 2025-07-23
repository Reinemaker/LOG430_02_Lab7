#!/bin/bash

echo "🧪 Testing CornerShop API Gateway..."
echo "======================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# API Gateway base URL
GATEWAY_URL="http://api.cornershop.localhost"
API_KEY="cornershop-api-key-2024"

# Function to test endpoint
test_endpoint() {
    local endpoint=$1
    local description=$2
    local method=${3:-GET}
    local data=${4:-""}
    
    echo -e "\n${YELLOW}Testing: $description${NC}"
    echo "Endpoint: $method $GATEWAY_URL$endpoint"
    
    if [ "$method" = "POST" ] && [ -n "$data" ]; then
        response=$(curl -s -w "\n%{http_code}" -X $method \
            -H "Content-Type: application/json" \
            -H "X-API-Key: $API_KEY" \
            -d "$data" \
            "$GATEWAY_URL$endpoint")
    else
        response=$(curl -s -w "\n%{http_code}" -X $method \
            -H "X-API-Key: $API_KEY" \
            "$GATEWAY_URL$endpoint")
    fi
    
    # Extract status code (last line)
    status_code=$(echo "$response" | tail -n1)
    # Extract response body (all lines except last)
    body=$(echo "$response" | head -n -1)
    
    if [ "$status_code" -ge 200 ] && [ "$status_code" -lt 300 ]; then
        echo -e "${GREEN}✅ Success (HTTP $status_code)${NC}"
        echo "Response: $body" | head -c 200
        [ ${#body} -gt 200 ] && echo "..."
    else
        echo -e "${RED}❌ Failed (HTTP $status_code)${NC}"
        echo "Response: $body"
    fi
}

# Function to test without API key (should fail)
test_without_api_key() {
    local endpoint=$1
    local description=$2
    
    echo -e "\n${YELLOW}Testing: $description (No API Key)${NC}"
    echo "Endpoint: GET $GATEWAY_URL$endpoint"
    
    response=$(curl -s -w "\n%{http_code}" "$GATEWAY_URL$endpoint")
    status_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n -1)
    
    if [ "$status_code" -eq 401 ]; then
        echo -e "${GREEN}✅ Correctly rejected (HTTP $status_code)${NC}"
        echo "Response: $body"
    else
        echo -e "${RED}❌ Should have been rejected (HTTP $status_code)${NC}"
        echo "Response: $body"
    fi
}

# Function to test with invalid API key (should fail)
test_invalid_api_key() {
    local endpoint=$1
    local description=$2
    
    echo -e "\n${YELLOW}Testing: $description (Invalid API Key)${NC}"
    echo "Endpoint: GET $GATEWAY_URL$endpoint"
    
    response=$(curl -s -w "\n%{http_code}" \
        -H "X-API-Key: invalid-key" \
        "$GATEWAY_URL$endpoint")
    status_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n -1)
    
    if [ "$status_code" -eq 403 ]; then
        echo -e "${GREEN}✅ Correctly rejected (HTTP $status_code)${NC}"
        echo "Response: $body"
    else
        echo -e "${RED}❌ Should have been rejected (HTTP $status_code)${NC}"
        echo "Response: $body"
    fi
}

# Wait for services to be ready
echo "⏳ Waiting for API Gateway to be ready..."
sleep 10

# Test 1: Health check (no API key required)
test_endpoint "/health" "Health Check"

# Test 2: Products endpoint with valid API key
test_endpoint "/api/products" "Get Products"

# Test 3: Customers endpoint with valid API key
test_endpoint "/api/customers" "Get Customers"

# Test 4: Cart endpoint with valid API key
test_endpoint "/api/cart" "Get Cart"

# Test 5: Orders endpoint with valid API key
test_endpoint "/api/orders" "Get Orders"

# Test 6: Products endpoint without API key (should fail)
test_without_api_key "/api/products" "Get Products"

# Test 7: Products endpoint with invalid API key (should fail)
test_invalid_api_key "/api/products" "Get Products"

# Test 8: Non-existent endpoint
test_endpoint "/api/nonexistent" "Non-existent Endpoint"

# Test 9: Root endpoint
test_endpoint "/" "Root Endpoint"

echo -e "\n${GREEN}🎉 API Gateway testing completed!${NC}"
echo ""
echo "📊 Summary:"
echo "• Health check should work without API key"
echo "• All API endpoints should require valid API key"
echo "• Invalid or missing API keys should be rejected"
echo "• Dynamic routing should work for all services"
echo ""
echo "📖 For detailed implementation, see API_GATEWAY_IMPLEMENTATION.md" 