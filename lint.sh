#!/bin/bash

# CornerShop Microservices Linting Script
# This script lints all microservices and shared components

# Don't exit on errors, handle them gracefully
set +e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== CornerShop Microservices Linting ===${NC}"
echo ""

# Install dotnet-format if not already installed
if ! command -v dotnet-format &> /dev/null; then
    echo -e "${YELLOW}Installing dotnet-format...${NC}"
    dotnet tool install -g dotnet-format
    echo -e "${GREEN}✓ dotnet-format installed${NC}"
else
    echo -e "${GREEN}✓ dotnet-format already installed${NC}"
fi

echo ""

# Define microservices and shared components
SERVICES=(
    "services/ProductService"
    "services/CustomerService"
    "services/CartService"
    "services/OrderService"
    "services/PaymentService"
    "services/StockService"
    "services/SalesService"
    "services/ReportingService"
    "services/SagaOrchestrator"
    "shared/CornerShop.Shared"
)

# Function to lint a service
lint_service() {
    local service_path=$1
    local service_name=$(basename "$service_path")
    
    echo -e "${YELLOW}Linting $service_name...${NC}"
    
    # Check if service directory exists
    if [ ! -d "$service_path" ]; then
        echo -e "${RED}✗ Service directory not found: $service_path${NC}"
        return 1
    fi
    
    # Check if .csproj file exists
    local csproj_files=( "$service_path"/*.csproj )
    if [ ! -f "${csproj_files[0]}" ]; then
        echo -e "${YELLOW}⚠ No .csproj file found in $service_path (skipping)${NC}"
        return 0  # Return 0 to continue processing other services
    fi
    
    # Restore packages
    echo -e "${YELLOW}  Restoring packages...${NC}"
    if ! dotnet restore "$service_path" > /dev/null 2>&1; then
        echo -e "${RED}✗ Package restore failed for $service_name${NC}"
        return 1
    fi
    
    # Check for formatting issues
    echo -e "${YELLOW}  Checking formatting...${NC}"
    if dotnet format --verify-no-changes --no-restore "$service_path" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ $service_name: No formatting issues${NC}"
        return 0
    else
        echo -e "${YELLOW}  Applying formatting corrections...${NC}"
        
        # Apply formatting
        if dotnet format --no-restore "$service_path" > /dev/null 2>&1; then
            echo -e "${GREEN}✓ $service_name: Formatting applied${NC}"
            
            # Verify changes
            if dotnet format --verify-no-changes --no-restore "$service_path" > /dev/null 2>&1; then
                echo -e "${GREEN}✓ $service_name: Formatting verified${NC}"
                return 0
            else
                echo -e "${RED}✗ $service_name: Some issues could not be automatically fixed${NC}"
                return 1
            fi
        else
            echo -e "${RED}✗ $service_name: Failed to apply formatting${NC}"
            return 1
        fi
    fi
}

# Function to run code analysis
run_code_analysis() {
    local service_path=$1
    local service_name=$(basename "$service_path")
    
    echo -e "${YELLOW}Running code analysis for $service_name...${NC}"
    
    # Check if .csproj file exists first
    local csproj_files=( "$service_path"/*.csproj )
    if [ ! -f "${csproj_files[0]}" ]; then
        echo -e "${YELLOW}⚠ Skipping analysis for $service_name (no .csproj file)${NC}"
        return 0
    fi
    
    # Build the project
    if dotnet build "$service_path" --no-restore --verbosity quiet > /dev/null 2>&1; then
        echo -e "${GREEN}✓ $service_name: Build successful${NC}"
        
        # Run code analysis (if available)
        if dotnet list "$service_path" package | grep -q "Microsoft.CodeAnalysis.NetAnalyzers"; then
            echo -e "${YELLOW}  Running analyzers...${NC}"
            if dotnet build "$service_path" --no-restore --verbosity normal 2>&1 | grep -E "(warning|error)" | head -10; then
                echo -e "${YELLOW}⚠ $service_name: Some warnings/errors found (showing first 10)${NC}"
            else
                echo -e "${GREEN}✓ $service_name: No analyzer issues${NC}"
            fi
        else
            echo -e "${YELLOW}⚠ $service_name: No analyzers configured${NC}"
        fi
    else
        echo -e "${RED}✗ $service_name: Build failed${NC}"
        return 1
    fi
}

# Function to check for common issues
check_common_issues() {
    local service_path=$1
    local service_name=$(basename "$service_path")
    
    echo -e "${YELLOW}Checking common issues for $service_name...${NC}"
    
    # Check if .csproj file exists first
    local csproj_files=( "$service_path"/*.csproj )
    if [ ! -f "${csproj_files[0]}" ]; then
        echo -e "${YELLOW}⚠ Skipping common issues check for $service_name (no .csproj file)${NC}"
        return 0
    fi
    
    # Check for TODO comments
    local todo_count=$(find "$service_path" -name "*.cs" -exec grep -l "TODO" {} \; | wc -l)
    if [ "$todo_count" -gt 0 ]; then
        echo -e "${YELLOW}⚠ $service_name: Found $todo_count files with TODO comments${NC}"
    fi
    
    # Check for FIXME comments
    local fixme_count=$(find "$service_path" -name "*.cs" -exec grep -l "FIXME" {} \; | wc -l)
    if [ "$fixme_count" -gt 0 ]; then
        echo -e "${YELLOW}⚠ $service_name: Found $fixme_count files with FIXME comments${NC}"
    fi
    
    # Check for hardcoded strings (basic check)
    local hardcoded_count=$(find "$service_path" -name "*.cs" -exec grep -l "\"[A-Z][A-Z_]*\"" {} \; | wc -l)
    if [ "$hardcoded_count" -gt 0 ]; then
        echo -e "${YELLOW}⚠ $service_name: Found $hardcoded_count files with potential hardcoded strings${NC}"
    fi
}

# Main execution
main() {
    local total_services=${#SERVICES[@]}
    local successful_lints=0
    local failed_lints=0
    
    echo -e "${BLUE}Found $total_services services to lint${NC}"
    echo ""
    
    # Lint each service
    for service in "${SERVICES[@]}"; do
        echo -e "${BLUE}=== Processing $service ===${NC}"
        
        if lint_service "$service"; then
            ((successful_lints++))
        else
            ((failed_lints++))
        fi
        
        echo ""
    done
    
    # Run additional analysis
    echo -e "${BLUE}=== Running Code Analysis ===${NC}"
    echo ""
    
    for service in "${SERVICES[@]}"; do
        run_code_analysis "$service"
        echo ""
    done
    
    # Check for common issues
    echo -e "${BLUE}=== Checking Common Issues ===${NC}"
    echo ""
    
    for service in "${SERVICES[@]}"; do
        check_common_issues "$service"
        echo ""
    done
    
    # Summary
    echo -e "${BLUE}=== Linting Summary ===${NC}"
    echo -e "${GREEN}✓ Successfully linted: $successful_lints services${NC}"
    
    if [ $failed_lints -gt 0 ]; then
        echo -e "${RED}✗ Failed to lint: $failed_lints services${NC}"
        echo ""
        echo -e "${YELLOW}To fix formatting issues manually:${NC}"
        echo "  dotnet format services/[ServiceName]"
        echo ""
        echo -e "${YELLOW}To check specific service:${NC}"
        echo "  dotnet format --verify-no-changes services/[ServiceName]"
        exit 1
    else
        echo -e "${GREEN}✓ All services linted successfully!${NC}"
        echo ""
        echo -e "${BLUE}=== Microservices Linting Complete ===${NC}"
    fi
}

# Function to show help
show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --help, -h           Show this help message"
    echo "  --service <name>     Lint specific service only"
    echo "  --format-only        Only check/apply formatting (skip analysis)"
    echo "  --analysis-only      Only run code analysis (skip formatting)"
    echo ""
    echo "Available services:"
    for service in "${SERVICES[@]}"; do
        echo "  - $(basename "$service")"
    done
    echo ""
    echo "Examples:"
    echo "  $0                    # Lint all services"
    echo "  $0 --service ProductService  # Lint only ProductService"
    echo "  $0 --format-only      # Only check formatting"
}

# Handle command line arguments
case "$1" in
    "--help"|"-h")
        show_help
        ;;
    "--service")
        if [ -z "$2" ]; then
            echo -e "${RED}Error: Service name required${NC}"
            show_help
            exit 1
        fi
        
        # Find the service
        local found_service=""
        for service in "${SERVICES[@]}"; do
            if [[ "$service" == *"$2"* ]]; then
                found_service="$service"
                break
            fi
        done
        
        if [ -z "$found_service" ]; then
            echo -e "${RED}Error: Service '$2' not found${NC}"
            show_help
            exit 1
        fi
        
        echo -e "${BLUE}=== Linting Single Service: $2 ===${NC}"
        echo ""
        lint_service "$found_service"
        run_code_analysis "$found_service"
        check_common_issues "$found_service"
        ;;
    "--format-only")
        echo -e "${BLUE}=== Formatting Only Mode ===${NC}"
        echo ""
        for service in "${SERVICES[@]}"; do
            lint_service "$service"
        done
        ;;
    "--analysis-only")
        echo -e "${BLUE}=== Analysis Only Mode ===${NC}"
        echo ""
        for service in "${SERVICES[@]}"; do
            run_code_analysis "$service"
            check_common_issues "$service"
        done
        ;;
    *)
        main
        ;;
esac 