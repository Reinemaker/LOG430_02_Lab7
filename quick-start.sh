#!/bin/bash

# CornerShop Load Testing Quick Start Script
# This script sets up the complete load testing environment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== CornerShop Load Testing Quick Start ===${NC}"
echo ""

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to install k6
install_k6() {
    echo -e "${YELLOW}Installing k6...${NC}"
    
    if command_exists k6; then
        echo -e "${GREEN}✓ k6 is already installed${NC}"
        return
    fi
    
    # Install k6
    sudo gpg -k
    sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
    echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
    sudo apt-get update
    sudo apt-get install -y k6
    
    echo -e "${GREEN}✓ k6 installed successfully${NC}"
}

# Function to install Docker
install_docker() {
    echo -e "${YELLOW}Installing Docker...${NC}"
    
    if command_exists docker; then
        echo -e "${GREEN}✓ Docker is already installed${NC}"
    else
        echo -e "${YELLOW}Installing Docker...${NC}"
        curl -fsSL https://get.docker.com -o get-docker.sh
        sudo sh get-docker.sh
        sudo usermod -aG docker $USER
        echo -e "${GREEN}✓ Docker installed successfully${NC}"
        echo -e "${YELLOW}⚠ Please log out and log back in for Docker group changes to take effect${NC}"
    fi
    
    if command_exists docker-compose; then
        echo -e "${GREEN}✓ Docker Compose is already installed${NC}"
    else
        echo -e "${YELLOW}Installing Docker Compose...${NC}"
        sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
        sudo chmod +x /usr/local/bin/docker-compose
        echo -e "${GREEN}✓ Docker Compose installed successfully${NC}"
    fi
}

# Function to start services
start_services() {
    echo -e "${YELLOW}Starting CornerShop microservices...${NC}"
    
    # Create necessary directories
    mkdir -p letsencrypt
    mkdir -p load-test-results
    
    # Check if microservices compose file exists
    if [ -f "docker-compose.microservices.yml" ]; then
        echo -e "${YELLOW}Starting microservices architecture...${NC}"
        docker-compose -f docker-compose.microservices.yml up -d --build
    else
        echo -e "${RED}❌ docker-compose.microservices.yml not found${NC}"
        echo -e "${YELLOW}Please ensure you're in the correct directory${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}✓ Services started successfully${NC}"
}

# Function to wait for services to be ready
wait_for_services() {
    echo -e "${YELLOW}Waiting for services to be ready...${NC}"
    
    local max_attempts=5
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        echo -e "${YELLOW}Attempt $attempt/$max_attempts - Checking services...${NC}"
        
        # Check if API Gateway is responding (microservices architecture)
        if curl -f -s http://api.cornershop.localhost/health > /dev/null 2>&1; then
            echo -e "${GREEN}✓ API Gateway is ready${NC}"
            break
        fi
        
        if [ $attempt -eq $max_attempts ]; then
            echo -e "${RED}✗ Services failed to start within expected time${NC}"
            echo -e "${YELLOW}Check logs with: docker-compose -f docker-compose.microservices.yml logs${NC}"
            exit 1
        fi
        
        echo -e "${YELLOW}Services not ready yet, waiting 10 seconds...${NC}"
        sleep 10
        attempt=$((attempt + 1))
    done
}

# Function to display access information
show_access_info() {
    echo ""
    echo -e "${BLUE}=== Access Information ===${NC}"
    
    echo -e "${GREEN}🌐 Microservices Architecture:${NC}"
    echo -e "${GREEN}API Gateway:${NC} http://api.cornershop.localhost"
    echo -e "${GREEN}Product Service:${NC} http://product.cornershop.localhost"
    echo -e "${GREEN}Customer Service:${NC} http://customer.cornershop.localhost"
    echo -e "${GREEN}Cart Service:${NC} http://cart.cornershop.localhost"
    echo -e "${GREEN}Order Service:${NC} http://order.cornershop.localhost"
    echo ""
    echo -e "${BLUE}=== Health Checks ===${NC}"
    echo -e "${YELLOW}API Gateway Health:${NC} http://api.cornershop.localhost/health"
    echo -e "${YELLOW}Product Service Health:${NC} http://product.cornershop.localhost/health"
    echo -e "${YELLOW}Customer Service Health:${NC} http://customer.cornershop.localhost/health"
    echo -e "${YELLOW}Cart Service Health:${NC} http://cart.cornershop.localhost/health"
    echo -e "${YELLOW}Order Service Health:${NC} http://order.cornershop.localhost/health"
    echo ""
    echo -e "${GREEN}Traefik Dashboard:${NC} http://traefik.localhost:8080"
    echo -e "${GREEN}Grafana Dashboard:${NC} http://localhost:3000 (admin/admin)"
    echo -e "${GREEN}Prometheus:${NC} http://localhost:9090"
    echo ""
}

# Function to run initial test
run_initial_test() {
    echo -e "${BLUE}=== Running Initial Load Test ===${NC}"
    
    if command_exists k6; then
        echo -e "${YELLOW}Running a quick load test to verify everything is working...${NC}"
        
        # Create a simple test
        cat > /tmp/quick-test.js << 'EOF'
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 5,
  duration: '30s',
};

const BASE_URL = __ENV.BASE_URL || 'http://api.cornershop.localhost';

export default function() {
  const response = http.get(`${BASE_URL}/health`);
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
}
EOF
        
        k6 run --env BASE_URL="http://api.cornershop.localhost" /tmp/quick-test.js
        rm -f /tmp/quick-test.js
        
        echo -e "${GREEN}✓ Initial load test completed${NC}"
    else
        echo -e "${YELLOW}⚠ k6 not available, skipping initial test${NC}"
    fi
}

# Function to show next steps
show_next_steps() {
    echo ""
    echo -e "${BLUE}=== Next Steps ===${NC}"
    echo -e "${GREEN}1.${NC} Run comprehensive load tests:"
    echo -e "   ${YELLOW}./run-load-tests.sh${NC}"
    echo ""
    echo -e "${GREEN}2.${NC} Run specific test scenarios:"
    echo -e "   ${YELLOW}k6 run load-tests/01-initial-load-test.js${NC}"
    echo -e "   ${YELLOW}k6 run load-tests/02-load-balancer-test.js${NC}"
    echo -e "   ${YELLOW}k6 run load-tests/03-cache-performance-test.js${NC}"
    echo ""
    echo -e "${GREEN}3.${NC} Monitor performance:"
    echo -e "   ${YELLOW}Open Grafana: http://localhost:3000${NC}"
    echo -e "   ${YELLOW}Check Traefik: http://traefik.localhost:8080${NC}"
    echo ""
    echo -e "${GREEN}4.${NC} View logs:"
    echo -e "   ${YELLOW}docker-compose -f docker-compose.microservices.yml logs -f${NC}"
    echo ""
    echo -e "${GREEN}5.${NC} Stop services:"
    echo -e "   ${YELLOW}docker-compose -f docker-compose.microservices.yml down${NC}"
    echo ""
}

# Main execution
main() {
    echo -e "${BLUE}Starting CornerShop Load Testing Setup...${NC}"
    echo ""
    
    # Check if we're in the right directory
    if [ ! -f "docker-compose.microservices.yml" ]; then
        echo -e "${RED}✗ docker-compose.microservices.yml not found${NC}"
        echo -e "${YELLOW}Please run this script from the CornerShop project directory${NC}"
        exit 1
    fi
    
    # Install dependencies
    install_docker
    install_k6
    
    # Start services
    start_services
    
    # Wait for services
    wait_for_services
    
    # Show access information
    show_access_info
    
    # Run initial test
    run_initial_test
    
    # Show next steps
    show_next_steps
    
    echo -e "${GREEN}=== Setup Complete! ===${NC}"
    echo -e "${YELLOW}Your CornerShop load testing environment is ready.${NC}"
}

# Handle command line arguments
case "$1" in
    "--help"|"-h")
        echo "Usage: $0 [OPTIONS]"
        echo ""
        echo "Options:"
        echo "  --help, -h           Show this help message"
        echo ""
        echo "This script will:"
        echo "  1. Install Docker and Docker Compose (if needed)"
        echo "  2. Install k6 load testing tool (if needed)"
        echo "  3. Start all CornerShop services"
        echo "  4. Run an initial load test"
        echo "  5. Display access information"
        echo ""
        ;;
    *)
        main
        ;;
esac 