#!/bin/bash

# CornerShop Load Testing Script
# This script runs comprehensive load tests using k6

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL=${BASE_URL:-"http://api.cornershop.localhost"}
ARCHITECTURE="microservices"
AUTH_TOKEN=${AUTH_TOKEN:-""}
OUTPUT_DIR="./load-test-results"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Create output directory
mkdir -p "$OUTPUT_DIR"

echo -e "${BLUE}=== CornerShop Load Testing Suite ===${NC}"
echo -e "${YELLOW}Architecture: ${ARCHITECTURE}${NC}"
echo -e "${YELLOW}Base URL: ${BASE_URL}${NC}"
echo -e "${YELLOW}Output Directory: ${OUTPUT_DIR}${NC}"
echo -e "${YELLOW}Timestamp: ${TIMESTAMP}${NC}"
echo ""

# Function to run a test
run_test() {
    local test_name=$1
    local test_file=$2
    local output_file="$OUTPUT_DIR/${test_name}_${TIMESTAMP}.json"
    
    echo -e "${GREEN}Running ${test_name}...${NC}"
    echo -e "${YELLOW}Output: ${output_file}${NC}"
    
    k6 run \
        --out json="$output_file" \
        --env BASE_URL="$BASE_URL" \
        --env AUTH_TOKEN="$AUTH_TOKEN" \
        "$test_file"
    
    echo -e "${GREEN}✓ ${test_name} completed${NC}"
    echo ""
}

# Function to check if services are ready
check_services() {
    echo -e "${BLUE}Checking service availability...${NC}"
    
    # Check if the application is responding
    if curl -f -s "$BASE_URL/health" > /dev/null; then
        echo -e "${GREEN}✓ Application is healthy${NC}"
    else
        echo -e "${RED}✗ Application is not responding${NC}"
        echo -e "${YELLOW}Make sure the application is running and accessible at ${BASE_URL}${NC}"
        exit 1
    fi
    
    # Check individual microservices
    echo -e "${BLUE}Checking microservices...${NC}"
    services=("product" "customer" "cart" "order")
    for service in "${services[@]}"; do
        if curl -f -s "http://${service}.cornershop.localhost/health" > /dev/null; then
            echo -e "${GREEN}✓ ${service}-service is healthy${NC}"
        else
            echo -e "${YELLOW}⚠ ${service}-service not accessible${NC}"
        fi
    done
    
    # Check if Traefik dashboard is accessible
    if curl -f -s "http://traefik.localhost:8080" > /dev/null; then
        echo -e "${GREEN}✓ Traefik dashboard is accessible${NC}"
    else
        echo -e "${YELLOW}⚠ Traefik dashboard not accessible (this is optional)${NC}"
    fi
    
    # Check if Grafana is accessible
    if curl -f -s "http://localhost:3000" > /dev/null; then
        echo -e "${GREEN}✓ Grafana is accessible${NC}"
    else
        echo -e "${YELLOW}⚠ Grafana not accessible (this is optional)${NC}"
    fi
    
    echo ""
}

# Function to display test results summary
show_summary() {
    echo -e "${BLUE}=== Test Results Summary ===${NC}"
    echo -e "${YELLOW}Architecture: ${ARCHITECTURE}${NC}"
    echo -e "${YELLOW}Results saved in: ${OUTPUT_DIR}${NC}"
    echo ""
    
    # List all result files
    if [ -d "$OUTPUT_DIR" ]; then
        echo -e "${GREEN}Generated result files:${NC}"
        ls -la "$OUTPUT_DIR"/*.json 2>/dev/null || echo "No result files found"
    fi
    
    echo ""
    echo -e "${BLUE}=== Access URLs ===${NC}"
    echo -e "${YELLOW}API Gateway: ${BASE_URL}${NC}"
    echo -e "${YELLOW}Product Service: http://product.cornershop.localhost${NC}"
    echo -e "${YELLOW}Customer Service: http://customer.cornershop.localhost${NC}"
    echo -e "${YELLOW}Cart Service: http://cart.cornershop.localhost${NC}"
    echo -e "${YELLOW}Order Service: http://order.cornershop.localhost${NC}"
    echo -e "${YELLOW}Traefik Dashboard: http://traefik.localhost:8080${NC}"
    echo -e "${YELLOW}Grafana Dashboard: http://localhost:3000 (admin/admin)${NC}"
    echo -e "${YELLOW}Prometheus: http://localhost:9090${NC}"
    echo ""
}

# Function to run stress test
run_stress_test() {
    echo -e "${BLUE}=== Running Stress Test ===${NC}"
    
    # Create a stress test configuration
    cat > /tmp/stress-test.js << 'EOF'
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '2m', target: 50 },   // Ramp up to 50 users
    { duration: '5m', target: 50 },   // Stay at 50 users
    { duration: '2m', target: 100 },  // Ramp up to 100 users
    { duration: '5m', target: 100 },  // Stay at 100 users
    { duration: '2m', target: 200 },  // Ramp up to 200 users
    { duration: '5m', target: 200 },  // Stay at 200 users
    { duration: '2m', target: 500 },  // Ramp up to 500 users
    { duration: '5m', target: 500 },  // Stay at 500 users
    { duration: '2m', target: 0 },    // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'], // 95% of requests must complete below 5s
    http_req_failed: ['rate<0.2'],     // Error rate must be less than 20%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://api.cornershop.localhost';

export default function() {
  const response = http.get(`${BASE_URL}/api/products`);
  
  check(response, {
    'status is 200': (r) => r.status === 200,
  });
  
  sleep(1);
}
EOF
    
    run_test "stress_test" "/tmp/stress-test.js"
    rm -f /tmp/stress-test.js
}

# Function to run fault tolerance test
run_fault_tolerance_test() {
    echo -e "${BLUE}=== Running Fault Tolerance Test ===${NC}"
    echo -e "${YELLOW}This test will simulate instance failures${NC}"
    echo -e "${YELLOW}Make sure you have multiple instances running${NC}"
    echo ""
    
    # Create a fault tolerance test
    cat > /tmp/fault-tolerance-test.js << 'EOF'
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '1m', target: 20 },   // Normal load
    { duration: '2m', target: 20 },   // Continue normal load
    { duration: '1m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.1'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://api.cornershop.localhost';

export default function() {
  const response = http.get(`${BASE_URL}/api/products`);
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 2s': (r) => r.timings.duration < 2000,
  });
  
  sleep(1);
}
EOF
    
    run_test "fault_tolerance_test" "/tmp/fault-tolerance-test.js"
    rm -f /tmp/fault-tolerance-test.js
}

# Main execution
main() {
    # Check if k6 is installed
    if ! command -v k6 &> /dev/null; then
        echo -e "${RED}✗ k6 is not installed${NC}"
        echo -e "${YELLOW}Please install k6: https://k6.io/docs/getting-started/installation/${NC}"
        exit 1
    fi
    
    # Check services
    check_services
    
    # Run tests
    echo -e "${BLUE}=== Starting Load Tests ===${NC}"
    
    # 1. Initial load test
    run_test "initial_load_test" "load-tests/01-initial-load-test.js"
    
    # 2. Load balancer test
    run_test "load_balancer_test" "load-tests/02-load-balancer-test.js"
    
    # 3. Cache performance test
    run_test "cache_performance_test" "load-tests/03-cache-performance-test.js"
    
    # 4. Stress test (optional)
    if [ "$1" = "--stress" ]; then
        run_stress_test
    fi
    
    # 5. Fault tolerance test (optional)
    if [ "$1" = "--fault-tolerance" ]; then
        run_fault_tolerance_test
    fi
    
    # Show summary
    show_summary
    
    echo -e "${GREEN}=== All tests completed successfully! ===${NC}"
}

# Handle command line arguments
case "$1" in
    "--stress")
        main --stress
        ;;
    "--fault-tolerance")
        main --fault-tolerance
        ;;
    "--help"|"-h")
        echo "Usage: $0 [OPTIONS]"
        echo ""
        echo "Options:"
        echo "  --stress              Run additional stress test"
        echo "  --fault-tolerance     Run fault tolerance test"
        echo "  --help, -h           Show this help message"
        echo ""
        echo "Environment variables:"
        echo "  BASE_URL             Base URL for the application (default: http://api.cornershop.localhost)"
        echo "  AUTH_TOKEN           JWT token for authentication (optional)"
        echo ""
        ;;
    *)
        main
        ;;
esac 