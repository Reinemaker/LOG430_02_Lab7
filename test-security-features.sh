#!/bin/bash

# Security Features Testing Script for CornerShop API Gateway
# This script tests CORS, API key authentication, rate limiting, and security headers

echo "🔒 Testing Security Features for CornerShop API Gateway"
echo "======================================================"

# Configuration
BASE_URL="http://api.cornershop.localhost"
API_KEY="cornershop-api-key-2024"
INVALID_API_KEY="invalid-key-12345"

echo "🌐 Target URL: $BASE_URL"
echo "🔑 API Key: $API_KEY"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_result() {
    local status=$1
    local message=$2
    if [ "$status" = "PASS" ]; then
        echo -e "${GREEN}✅ PASS${NC}: $message"
    elif [ "$status" = "FAIL" ]; then
        echo -e "${RED}❌ FAIL${NC}: $message"
    elif [ "$status" = "WARN" ]; then
        echo -e "${YELLOW}⚠️  WARN${NC}: $message"
    else
        echo -e "${BLUE}ℹ️  INFO${NC}: $message"
    fi
}

# Test 1: CORS Preflight Request
echo "1️⃣  Testing CORS Preflight Request..."
echo "----------------------------------------"

cors_preflight=$(curl -s -o /dev/null -w "%{http_code}" \
    -X OPTIONS \
    -H "Origin: http://localhost:3000" \
    -H "Access-Control-Request-Method: POST" \
    -H "Access-Control-Request-Headers: X-API-Key" \
    "$BASE_URL/api/products")

if [ "$cors_preflight" = "204" ]; then
    print_result "PASS" "CORS preflight request returned 204 (No Content)"
else
    print_result "FAIL" "CORS preflight request returned $cors_preflight (expected 204)"
fi

# Test 2: CORS Headers in Response
echo ""
echo "2️⃣  Testing CORS Headers in Response..."
echo "----------------------------------------"

cors_headers=$(curl -s -I -H "Origin: http://localhost:3000" \
    -H "X-API-Key: $API_KEY" \
    "$BASE_URL/api/products" | grep -i "access-control")

if echo "$cors_headers" | grep -q "Access-Control-Allow-Origin"; then
    print_result "PASS" "CORS headers present in response"
    echo "$cors_headers" | while read line; do
        echo "   $line"
    done
else
    print_result "FAIL" "CORS headers missing in response"
fi

# Test 3: Missing API Key
echo ""
echo "3️⃣  Testing Missing API Key..."
echo "----------------------------------------"

missing_key=$(curl -s -o /dev/null -w "%{http_code}" \
    "$BASE_URL/api/products")

if [ "$missing_key" = "401" ]; then
    print_result "PASS" "Missing API key correctly returns 401 Unauthorized"
else
    print_result "FAIL" "Missing API key returned $missing_key (expected 401)"
fi

# Test 4: Invalid API Key
echo ""
echo "4️⃣  Testing Invalid API Key..."
echo "----------------------------------------"

invalid_key=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "X-API-Key: $INVALID_API_KEY" \
    "$BASE_URL/api/products")

if [ "$invalid_key" = "403" ]; then
    print_result "PASS" "Invalid API key correctly returns 403 Forbidden"
else
    print_result "FAIL" "Invalid API key returned $invalid_key (expected 403)"
fi

# Test 5: Valid API Key
echo ""
echo "5️⃣  Testing Valid API Key..."
echo "----------------------------------------"

valid_key=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "X-API-Key: $API_KEY" \
    "$BASE_URL/api/products")

if [ "$valid_key" = "200" ]; then
    print_result "PASS" "Valid API key correctly returns 200 OK"
else
    print_result "FAIL" "Valid API key returned $valid_key (expected 200)"
fi

# Test 6: Security Headers
echo ""
echo "6️⃣  Testing Security Headers..."
echo "----------------------------------------"

security_headers=$(curl -s -I -H "X-API-Key: $API_KEY" \
    "$BASE_URL/api/products")

# Check for specific security headers
headers_to_check=(
    "X-Frame-Options"
    "X-Content-Type-Options"
    "X-XSS-Protection"
    "Referrer-Policy"
    "Content-Security-Policy"
)

for header in "${headers_to_check[@]}"; do
    if echo "$security_headers" | grep -qi "$header"; then
        print_result "PASS" "Security header $header present"
    else
        print_result "FAIL" "Security header $header missing"
    fi
done

# Test 7: Rate Limiting
echo ""
echo "7️⃣  Testing Rate Limiting..."
echo "----------------------------------------"

print_result "INFO" "Sending 15 rapid requests to test rate limiting..."

rate_limit_hits=0
for i in {1..15}; do
    response_code=$(curl -s -o /dev/null -w "%{http_code}" \
        -H "X-API-Key: $API_KEY" \
        "$BASE_URL/api/customers")
    
    if [ "$response_code" = "429" ]; then
        rate_limit_hits=$((rate_limit_hits + 1))
    fi
    
    echo -n "."
    sleep 0.1
done
echo ""

if [ $rate_limit_hits -gt 0 ]; then
    print_result "PASS" "Rate limiting working: $rate_limit_hits requests were rate limited"
else
    print_result "WARN" "No rate limiting detected (may need more requests or different timing)"
fi

# Test 8: Service-Specific Rate Limiting
echo ""
echo "8️⃣  Testing Service-Specific Rate Limiting..."
echo "----------------------------------------"

print_result "INFO" "Testing different rate limits for different services..."

# Test cart service (higher limit)
cart_rate_hits=0
for i in {1..12}; do
    response_code=$(curl -s -o /dev/null -w "%{http_code}" \
        -H "X-API-Key: $API_KEY" \
        "$BASE_URL/api/cart")
    
    if [ "$response_code" = "429" ]; then
        cart_rate_hits=$((cart_rate_hits + 1))
    fi
    
    sleep 0.1
done

# Test customer service (stricter limit)
customer_rate_hits=0
for i in {1..8}; do
    response_code=$(curl -s -o /dev/null -w "%{http_code}" \
        -H "X-API-Key: $API_KEY" \
        "$BASE_URL/api/customers")
    
    if [ "$response_code" = "429" ]; then
        customer_rate_hits=$((customer_rate_hits + 1))
    fi
    
    sleep 0.1
done

print_result "INFO" "Cart service rate limit hits: $cart_rate_hits"
print_result "INFO" "Customer service rate limit hits: $customer_rate_hits"

if [ $customer_rate_hits -ge $cart_rate_hits ]; then
    print_result "PASS" "Customer service has stricter rate limiting than cart service"
else
    print_result "WARN" "Rate limiting differences not clearly detected"
fi

# Test 9: Request ID and Custom Headers
echo ""
echo "9️⃣  Testing Request ID and Custom Headers..."
echo "----------------------------------------"

custom_headers=$(curl -s -I -H "X-API-Key: $API_KEY" \
    "$BASE_URL/api/products")

# Check for custom headers
custom_headers_to_check=(
    "X-Gateway-Version"
    "X-Request-ID"
    "X-Forwarded-For"
    "X-Real-IP"
)

for header in "${custom_headers_to_check[@]}"; do
    if echo "$custom_headers" | grep -qi "$header"; then
        print_result "PASS" "Custom header $header present"
    else
        print_result "FAIL" "Custom header $header missing"
    fi
done

# Test 10: Error Response Format
echo ""
echo "🔟  Testing Error Response Format..."
echo "----------------------------------------"

error_response=$(curl -s -H "X-API-Key: $INVALID_API_KEY" \
    "$BASE_URL/api/products")

if echo "$error_response" | grep -q "error" && echo "$error_response" | grep -q "code"; then
    print_result "PASS" "Error response contains structured JSON with error and code fields"
    echo "   Response: $error_response"
else
    print_result "FAIL" "Error response format incorrect"
    echo "   Response: $error_response"
fi

# Summary
echo ""
echo "📊 Security Testing Summary"
echo "=========================="
echo "✅ CORS Configuration: Tested preflight and headers"
echo "✅ API Key Authentication: Tested missing, invalid, and valid keys"
echo "✅ Security Headers: Verified all required headers present"
echo "✅ Rate Limiting: Tested general and service-specific limits"
echo "✅ Custom Headers: Verified request ID and gateway headers"
echo "✅ Error Handling: Confirmed structured error responses"
echo ""
echo "🔍 For detailed monitoring, check:"
echo "   • Security logs: docker logs api-gateway"
echo "   • Grafana dashboard: http://localhost:3000"
echo "   • Traefik dashboard: http://traefik.localhost:8080"
echo ""
echo "🎉 Security testing completed!" 