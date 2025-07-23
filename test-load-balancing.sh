#!/bin/bash

# Load Balancing Test Script for Cart Service
# This script tests the round-robin load balancing across multiple cart service instances

echo "🧪 Testing Load Balancing for Cart Service"
echo "=========================================="

# Configuration
BASE_URL="http://cart.cornershop.localhost"
API_KEY="cornershop-api-key-2024"
TOTAL_REQUESTS=30
DELAY=0.5

echo "📊 Making $TOTAL_REQUESTS requests to test load distribution..."
echo "🌐 Target URL: $BASE_URL"
echo "⏱️  Delay between requests: ${DELAY}s"
echo ""

# Create a temporary file to store responses
TEMP_FILE=$(mktemp)

# Make requests and capture responses
for i in $(seq 1 $TOTAL_REQUESTS); do
    echo -n "Request $i/$TOTAL_REQUESTS: "
    
    # Make request and capture response
    response=$(curl -s -w "\n%{http_code}\n%{time_total}" \
        -H "X-API-Key: $API_KEY" \
        -H "Content-Type: application/json" \
        "$BASE_URL/health")
    
    # Extract response body, status code, and timing
    body=$(echo "$response" | head -n -2)
    status=$(echo "$response" | tail -n 2 | head -n 1)
    timing=$(echo "$response" | tail -n 1)
    
    # Try to extract instance information from response
    instance_info=$(echo "$body" | grep -o '"instance":[^,}]*' | cut -d':' -f2 | tr -d '"' || echo "unknown")
    
    echo "Status: $status, Time: ${timing}s, Instance: $instance_info"
    
    # Store instance info for analysis
    echo "$instance_info" >> "$TEMP_FILE"
    
    sleep $DELAY
done

echo ""
echo "📈 Load Distribution Analysis:"
echo "=============================="

# Count requests per instance
echo "Requests per instance:"
sort "$TEMP_FILE" | uniq -c | while read count instance; do
    percentage=$(echo "scale=1; $count * 100 / $TOTAL_REQUESTS" | bc)
    echo "  Instance $instance: $count requests ($percentage%)"
done

echo ""
echo "✅ Load balancing test completed!"
echo "💡 Check Grafana dashboard for detailed metrics: http://localhost:3000"

# Clean up
rm "$TEMP_FILE" 