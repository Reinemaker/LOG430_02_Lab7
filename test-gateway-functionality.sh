#!/bin/bash

echo "🧪 Testing CornerShop API Gateway Functionality"
echo "================================================"
echo "This script verifies that the API Gateway is working correctly"
echo "and all endpoints are accessible through the gateway."
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
GATEWAY_URL="http://api.cornershop.localhost"
API_KEY="cornershop-api-key-2024"
TEST_RESULTS=()

# Function to test endpoint and record result
test_endpoint() {
    local endpoint=$1
    local description=$2
    local method=${3:-GET}
    local data=${4:-""}
    local expected_status=${5:-200}
    
    echo -e "\n${BLUE}Testing: $description${NC}"
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
    
    if [ "$status_code" -eq "$expected_status" ]; then
        echo -e "${GREEN}✅ PASS (HTTP $status_code)${NC}"
        TEST_RESULTS+=("PASS: $description")
        echo "Response: $body" | head -c 200
        [ ${#body} -gt 200 ] && echo "..."
    else
        echo -e "${RED}❌ FAIL (HTTP $status_code, expected $expected_status)${NC}"
        TEST_RESULTS+=("FAIL: $description (HTTP $status_code)")
        echo "Response: $body"
    fi
}

# Function to test authentication
test_authentication() {
    local endpoint=$1
    local description=$2
    
    echo -e "\n${BLUE}Testing Authentication: $description${NC}"
    echo "Endpoint: GET $GATEWAY_URL$endpoint"
    
    # Test without API key
    response=$(curl -s -w "\n%{http_code}" "$GATEWAY_URL$endpoint")
    status_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n -1)
    
    if [ "$status_code" -eq 401 ]; then
        echo -e "${GREEN}✅ PASS: Correctly rejected without API key (HTTP $status_code)${NC}"
        TEST_RESULTS+=("PASS: $description - No API key")
    else
        echo -e "${RED}❌ FAIL: Should have been rejected without API key (HTTP $status_code)${NC}"
        TEST_RESULTS+=("FAIL: $description - No API key (HTTP $status_code)")
    fi
    
    # Test with invalid API key
    response=$(curl -s -w "\n%{http_code}" \
        -H "X-API-Key: invalid-key" \
        "$GATEWAY_URL$endpoint")
    status_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n -1)
    
    if [ "$status_code" -eq 403 ]; then
        echo -e "${GREEN}✅ PASS: Correctly rejected with invalid API key (HTTP $status_code)${NC}"
        TEST_RESULTS+=("PASS: $description - Invalid API key")
    else
        echo -e "${RED}❌ FAIL: Should have been rejected with invalid API key (HTTP $status_code)${NC}"
        TEST_RESULTS+=("FAIL: $description - Invalid API key (HTTP $status_code)")
    fi
}

# Function to test rate limiting
test_rate_limiting() {
    local endpoint=$1
    local description=$2
    
    echo -e "\n${BLUE}Testing Rate Limiting: $description${NC}"
    echo "Making multiple rapid requests to test rate limiting..."
    
    # Make multiple requests rapidly
    for i in {1..15}; do
        response=$(curl -s -w "\n%{http_code}" \
            -H "X-API-Key: $API_KEY" \
            "$GATEWAY_URL$endpoint")
        status_code=$(echo "$response" | tail -n1)
        
        if [ "$status_code" -eq 429 ]; then
            echo -e "${GREEN}✅ PASS: Rate limiting working (HTTP 429 on request $i)${NC}"
            TEST_RESULTS+=("PASS: $description - Rate limiting")
            return 0
        fi
    done
    
    echo -e "${YELLOW}⚠️  WARNING: Rate limiting may not be working (no 429 responses)${NC}"
    TEST_RESULTS+=("WARN: $description - Rate limiting")
}

# Wait for services to be ready
echo "⏳ Waiting for API Gateway to be ready..."
sleep 5

echo -e "\n${YELLOW}=== HEALTH CHECK TESTS ===${NC}"
test_endpoint "/health" "Gateway Health Check" "GET" "" 200

echo -e "\n${YELLOW}=== AUTHENTICATION TESTS ===${NC}"
test_authentication "/api/products" "Products endpoint authentication"
test_authentication "/api/customers" "Customers endpoint authentication"
test_authentication "/api/carts" "Cart endpoint authentication"
test_authentication "/api/orders" "Orders endpoint authentication"

echo -e "\n${YELLOW}=== API ENDPOINT TESTS ===${NC}"
test_endpoint "/api/products" "Get Products"
test_endpoint "/api/customers" "Get Customers"
test_endpoint "/api/carts" "Get Cart (requires customerId parameter)"
test_endpoint "/api/orders" "Get Orders"

echo -e "\n${YELLOW}=== ERROR HANDLING TESTS ===${NC}"
test_endpoint "/api/nonexistent" "Non-existent endpoint" "GET" "" 404
test_endpoint "/api/products/invalid-id" "Invalid product ID" "GET" "" 404

echo -e "\n${YELLOW}=== RATE LIMITING TESTS ===${NC}"
test_rate_limiting "/api/products" "Products endpoint rate limiting"

echo -e "\n${YELLOW}=== GATEWAY HEADERS TESTS ===${NC}"
echo -e "\n${BLUE}Testing Gateway Headers${NC}"
response=$(curl -s -I -H "X-API-Key: $API_KEY" "$GATEWAY_URL/api/products")
echo "$response" | grep -E "(X-Gateway-Version|X-Request-ID|X-Forwarded-For)" || echo "Headers not found"

echo -e "\n${YELLOW}=== CORS TESTS ===${NC}"
echo -e "\n${BLUE}Testing CORS Headers${NC}"
response=$(curl -s -I -H "X-API-Key: $API_KEY" "$GATEWAY_URL/api/products")
echo "$response" | grep -E "(Access-Control-Allow-Origin|Access-Control-Allow-Methods)" || echo "CORS headers not found"

# Print summary
echo -e "\n${YELLOW}=== TEST SUMMARY ===${NC}"
echo "Total tests run: ${#TEST_RESULTS[@]}"
echo ""

pass_count=0
fail_count=0
warn_count=0

for result in "${TEST_RESULTS[@]}"; do
    if [[ $result == PASS* ]]; then
        echo -e "${GREEN}$result${NC}"
        ((pass_count++))
    elif [[ $result == FAIL* ]]; then
        echo -e "${RED}$result${NC}"
        ((fail_count++))
    elif [[ $result == WARN* ]]; then
        echo -e "${YELLOW}$result${NC}"
        ((warn_count++))
    fi
done

echo -e "\n${YELLOW}=== FINAL RESULTS ===${NC}"
echo -e "${GREEN}✅ Passed: $pass_count${NC}"
echo -e "${RED}❌ Failed: $fail_count${NC}"
echo -e "${YELLOW}⚠️  Warnings: $warn_count${NC}"

if [ $fail_count -eq 0 ]; then
    echo -e "\n${GREEN}🎉 All critical tests passed! API Gateway is working correctly.${NC}"
    exit 0
else
    echo -e "\n${RED}⚠️  Some tests failed. Please check the API Gateway configuration.${NC}"
    exit 1
fi 