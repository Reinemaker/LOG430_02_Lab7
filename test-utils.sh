#!/bin/bash

# CornerShop Test Utilities Library
# This file contains common functions and configurations used across all test scripts

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Common configuration
DEFAULT_TIMEOUT=10
DEFAULT_API_KEY="cornershop-api-key-2024"
BASE_URL="http://localhost"
API_GATEWAY_URL="${BASE_URL}/api"
TIMEOUT=${TIMEOUT:-$DEFAULT_TIMEOUT}
API_KEY=${API_KEY:-$DEFAULT_API_KEY}

# Function to print section headers
print_section() {
    echo -e "${YELLOW}--- $1 ---${NC}"
}

# Function to print success message
print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

# Function to print error message
print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Function to print info message
print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

# Function to print warning message
print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

# Function to check service health
check_service_health() {
    local service_name=$1
    local health_url=$2
    local timeout=${3:-$DEFAULT_TIMEOUT}
    
    print_section "Health Check: $service_name"
    
    if curl -s -f --max-time $timeout "$health_url" > /dev/null; then
        print_success "$service_name is healthy"
        return 0
    else
        print_error "$service_name is not responding"
        return 1
    fi
}

# Function to test API endpoint
test_api_endpoint() {
    local method=$1
    local url=$2
    local expected_status=${3:-200}
    local timeout=${4:-$DEFAULT_TIMEOUT}
    local headers=${5:-""}
    
    local curl_cmd="curl -s -f --max-time $timeout -X $method"
    
    if [ ! -z "$headers" ]; then
        curl_cmd="$curl_cmd $headers"
    fi
    
    curl_cmd="$curl_cmd '$url'"
    
    if eval $curl_cmd > /dev/null; then
        print_success "$method $url - OK"
        return 0
    else
        print_error "$method $url - Failed"
        return 1
    fi
}

# Function to test API endpoint with API key
test_api_endpoint_with_key() {
    local method=$1
    local url=$2
    local api_key=${3:-$DEFAULT_API_KEY}
    local expected_status=${4:-200}
    local timeout=${5:-$DEFAULT_TIMEOUT}
    
    local headers="-H 'Content-Type: application/json' -H 'X-API-Key: $api_key'"
    test_api_endpoint "$method" "$url" "$expected_status" "$timeout" "$headers"
}

# Function to wait for service to be ready
wait_for_service() {
    local service_name=$1
    local url=$2
    local timeout=${3:-30}
    local interval=${4:-2}
    
    print_section "Waiting for $service_name to be ready"
    
    local elapsed=0
    while [ $elapsed -lt $timeout ]; do
        if curl -s -f --max-time 5 "$url" > /dev/null 2>&1; then
            print_success "$service_name is ready"
            return 0
        fi
        
        print_info "Waiting for $service_name... ($elapsed/$timeout seconds)"
        sleep $interval
        elapsed=$((elapsed + interval))
    done
    
    print_error "$service_name failed to start within $timeout seconds"
    return 1
}

# Function to validate JSON response
validate_json_response() {
    local response=$1
    
    if echo "$response" | jq . > /dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Function to get JSON field value
get_json_field() {
    local json=$1
    local field=$2
    
    echo "$json" | jq -r ".$field" 2>/dev/null
}

# Function to compare values
assert_equal() {
    local expected=$1
    local actual=$2
    local message=${3:-"Values should be equal"}
    
    if [ "$expected" = "$actual" ]; then
        print_success "$message"
        return 0
    else
        print_error "$message (expected: $expected, actual: $actual)"
        return 1
    fi
}

# Function to check if value contains substring
assert_contains() {
    local haystack=$1
    local needle=$2
    local message=${3:-"Value should contain substring"}
    
    if echo "$haystack" | grep -q "$needle"; then
        print_success "$message"
        return 0
    else
        print_error "$message (haystack: $haystack, needle: $needle)"
        return 1
    fi
}

# Function to run test with timeout
run_test_with_timeout() {
    local command=$1
    local timeout=${2:-30}
    
    timeout $timeout bash -c "$command"
    local exit_code=$?
    
    if [ $exit_code -eq 124 ]; then
        print_error "Test timed out after $timeout seconds"
        return 1
    else
        return $exit_code
    fi
}

# Function to cleanup test resources
cleanup_test_resources() {
    print_section "Cleaning up test resources"
    
    # Add cleanup logic here if needed
    print_success "Cleanup completed"
}

# Function to setup test environment
setup_test_environment() {
    print_section "Setting up test environment"
    
    # Add setup logic here if needed
    print_success "Test environment ready"
}

# Function to print test summary
print_test_summary() {
    local passed=$1
    local failed=$2
    local total=$3
    
    print_section "Test Summary"
    echo "Total tests: $total"
    echo -e "${GREEN}Passed: $passed${NC}"
    echo -e "${RED}Failed: $failed${NC}"
    
    if [ $failed -eq 0 ]; then
        print_success "All tests passed!"
        return 0
    else
        print_error "Some tests failed"
        return 1
    fi
}

# Export functions for use in other scripts
export -f print_section print_success print_error print_info print_warning
export -f check_service_health test_api_endpoint test_api_endpoint_with_key
export -f wait_for_service validate_json_response get_json_field
export -f assert_equal assert_contains run_test_with_timeout
export -f cleanup_test_resources setup_test_environment print_test_summary 