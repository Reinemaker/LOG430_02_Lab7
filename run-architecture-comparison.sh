#!/bin/bash

# CornerShop Architecture Comparison Script
# This script compares the performance of direct API calls vs API Gateway calls

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

# Configuration
DIRECT_BASE_URL=${DIRECT_BASE_URL:-"http://cornershop.localhost"}
GATEWAY_BASE_URL=${GATEWAY_BASE_URL:-"http://api.cornershop.localhost"}
API_KEY=${API_KEY:-"cornershop-api-key-2024"}
OUTPUT_DIR="./comparison-results"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Create output directory
mkdir -p "$OUTPUT_DIR"

echo -e "${PURPLE}=== CornerShop Architecture Comparison Test ===${NC}"
echo -e "${YELLOW}Direct API URL: ${DIRECT_BASE_URL}${NC}"
echo -e "${YELLOW}Gateway API URL: ${GATEWAY_BASE_URL}${NC}"
echo -e "${YELLOW}Output Directory: ${OUTPUT_DIR}${NC}"
echo -e "${YELLOW}Timestamp: ${TIMESTAMP}${NC}"
echo ""

# Function to check service availability
check_services() {
    echo -e "${BLUE}Checking service availability...${NC}"
    
    # Check direct API
    if curl -f -s "$DIRECT_BASE_URL/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Direct API is accessible${NC}"
    else
        echo -e "${RED}✗ Direct API is not accessible${NC}"
        echo -e "${YELLOW}Make sure the old architecture is running at ${DIRECT_BASE_URL}${NC}"
        exit 1
    fi
    
    # Check API Gateway
    if curl -f -s -H "X-API-Key: $API_KEY" "$GATEWAY_BASE_URL/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ API Gateway is accessible${NC}"
    else
        echo -e "${RED}✗ API Gateway is not accessible${NC}"
        echo -e "${YELLOW}Make sure the new architecture is running at ${GATEWAY_BASE_URL}${NC}"
        exit 1
    fi
    
    echo ""
}

# Function to run comparison test
run_comparison_test() {
    local test_name=$1
    local output_file="$OUTPUT_DIR/${test_name}_${TIMESTAMP}.json"
    
    echo -e "${GREEN}Running ${test_name}...${NC}"
    echo -e "${YELLOW}Output: ${output_file}${NC}"
    
    k6 run \
        --out json="$output_file" \
        --env DIRECT_BASE_URL="$DIRECT_BASE_URL" \
        --env GATEWAY_BASE_URL="$GATEWAY_BASE_URL" \
        --env API_KEY="$API_KEY" \
        "load-tests/04-architecture-comparison-test.js"
    
    echo -e "${GREEN}✓ ${test_name} completed${NC}"
    echo ""
}

# Function to analyze results
analyze_results() {
    local results_file="$OUTPUT_DIR/architecture_comparison_${TIMESTAMP}.json"
    
    if [ ! -f "$results_file" ]; then
        echo -e "${RED}No results file found: ${results_file}${NC}"
        return 1
    fi
    
    echo -e "${BLUE}=== Analysis Results ===${NC}"
    
    # Extract metrics using jq (if available)
    if command -v jq &> /dev/null; then
        echo -e "${YELLOW}Detailed metrics analysis:${NC}"
        
        # Extract key metrics
        local total_requests=$(jq '.metrics.http_reqs.values.count' "$results_file" 2>/dev/null || echo "N/A")
        local avg_response_time=$(jq '.metrics.http_req_duration.values.avg' "$results_file" 2>/dev/null || echo "N/A")
        local p95_response_time=$(jq '.metrics.http_req_duration.values["p(95)"]' "$results_file" 2>/dev/null || echo "N/A")
        local error_rate=$(jq '.metrics.http_req_failed.values.rate' "$results_file" 2>/dev/null || echo "N/A")
        
        echo -e "${GREEN}Total Requests: ${total_requests}${NC}"
        echo -e "${GREEN}Average Response Time: ${avg_response_time}ms${NC}"
        echo -e "${GREEN}95th Percentile Response Time: ${p95_response_time}ms${NC}"
        echo -e "${GREEN}Error Rate: ${error_rate}%${NC}"
    else
        echo -e "${YELLOW}Install jq for detailed analysis: sudo apt-get install jq${NC}"
    fi
    
    echo ""
}

# Function to generate comparison report
generate_report() {
    local report_file="$OUTPUT_DIR/architecture_comparison_report_${TIMESTAMP}.md"
    
    echo -e "${BLUE}Generating comparison report...${NC}"
    
    cat > "$report_file" << EOF
# CornerShop Architecture Comparison Report

**Generated:** $(date)
**Test Duration:** 8 minutes (1m warmup + 2m normal + 2m medium + 2m high + 1m cooldown)

## Test Configuration

- **Direct API URL:** $DIRECT_BASE_URL
- **Gateway API URL:** $GATEWAY_BASE_URL
- **API Key:** $API_KEY
- **Load Pattern:** 10 → 50 → 100 → 200 → 0 users

## Architecture Comparison

### Old Architecture (Direct API Calls)
- **Pros:**
  - Lower latency (no gateway overhead)
  - Simpler request path
  - Direct service communication
  
- **Cons:**
  - No centralized security
  - No request/response transformation
  - Limited observability
  - No rate limiting
  - No unified error handling

### New Architecture (API Gateway)
- **Pros:**
  - Centralized security (API key validation)
  - Unified error handling
  - Rate limiting and throttling
  - Request/response transformation
  - Enhanced observability
  - CORS handling
  - Load balancing capabilities
  
- **Cons:**
  - Additional latency (gateway overhead)
  - Single point of failure (if not properly configured)
  - Increased complexity

## Performance Metrics

### Latency Comparison
- **Direct API:** Expected < 2000ms (95th percentile)
- **Gateway API:** Expected < 3000ms (95th percentile)
- **Overhead:** ~1000ms additional latency through gateway

### Error Rate Comparison
- **Direct API:** Expected < 5%
- **Gateway API:** Expected < 5%
- **Improvement:** Better error handling and retry logic

### Availability Comparison
- **Direct API:** Service-level availability
- **Gateway API:** Enhanced with health checks and circuit breakers

## Observability Improvements

### Enhanced Visibility
1. **Request Tracing:** Gateway adds request IDs for tracking
2. **Centralized Logging:** All requests logged through gateway
3. **Metrics Collection:** Prometheus metrics for all services
4. **Health Monitoring:** Gateway health checks for all services

### Monitoring Capabilities
- **Real-time Metrics:** Response times, error rates, throughput
- **Service Discovery:** Automatic service registration
- **Load Balancing:** Traffic distribution across instances
- **Circuit Breaking:** Automatic failure detection and recovery

## Security Enhancements

### Authentication & Authorization
- **API Key Validation:** Centralized at gateway level
- **Rate Limiting:** Per-client and per-endpoint limits
- **Request Validation:** Input sanitization and validation
- **CORS Management:** Cross-origin request handling

## Recommendations

### For Production Use
1. **Use API Gateway Architecture** for better security and observability
2. **Implement Circuit Breakers** for improved fault tolerance
3. **Add Request Caching** to reduce latency
4. **Monitor Gateway Performance** to optimize overhead
5. **Implement Service Mesh** for advanced traffic management

### Performance Optimization
1. **Gateway Caching:** Cache frequently requested data
2. **Connection Pooling:** Optimize database connections
3. **Load Balancing:** Distribute traffic efficiently
4. **Compression:** Enable response compression

## Conclusion

The API Gateway architecture provides significant improvements in:
- **Security:** Centralized authentication and authorization
- **Observability:** Enhanced monitoring and tracing
- **Reliability:** Better error handling and fault tolerance
- **Scalability:** Load balancing and rate limiting

While there is a small latency overhead (~1000ms), the benefits in security, observability, and maintainability outweigh this cost for production environments.

EOF

    echo -e "${GREEN}Report generated: ${report_file}${NC}"
    echo ""
}

# Function to display URLs
show_urls() {
    echo -e "${BLUE}=== Access URLs ===${NC}"
    echo -e "${YELLOW}Direct API: ${DIRECT_BASE_URL}${NC}"
    echo -e "${YELLOW}API Gateway: ${GATEWAY_BASE_URL}${NC}"
    echo -e "${YELLOW}Grafana Dashboard: http://localhost:3000 (admin/admin)${NC}"
    echo -e "${YELLOW}Prometheus: http://localhost:9090${NC}"
    echo -e "${YELLOW}Traefik Dashboard: http://traefik.localhost:8080${NC}"
    echo ""
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
    
    # Run comparison test
    echo -e "${BLUE}=== Starting Architecture Comparison Test ===${NC}"
    run_comparison_test "architecture_comparison"
    
    # Analyze results
    analyze_results
    
    # Generate report
    generate_report
    
    # Show URLs
    show_urls
    
    echo -e "${GREEN}🎉 Architecture comparison completed!${NC}"
    echo -e "${YELLOW}Check the results in: ${OUTPUT_DIR}${NC}"
}

# Run main function
main 