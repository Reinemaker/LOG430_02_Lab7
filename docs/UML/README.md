# CornerShop UML Diagrams

This directory contains comprehensive UML diagrams for the CornerShop system, following the 4+1 architectural view model.

## Diagram Overview

### 4+1 Architectural Views

#### 1. **Logical View** (`01-logical-view.puml`)
- **Purpose**: Shows the main components and their relationships
- **Scope**: System-wide component architecture
- **Key Elements**:
  - Client Layer (Web, Mobile, API clients)
  - API Gateway Layer (Load balancer, authentication, rate limiting)
  - Microservices Layer (Product, Customer, Order, Analytics domains)
  - Infrastructure Layer (Saga management, event management)
  - Data Layer (MongoDB, Redis, Event Store)
- **Use Case**: Understanding system architecture and component interactions

#### 2. **Process View** (`02-process-view.puml`)
- **Purpose**: Shows runtime behavior and process interactions
- **Scope**: Process-level interactions and data flow
- **Key Elements**:
  - Client processes (Web browser, mobile app, API client)
  - Gateway processes (API gateway, load balancer, rate limiter)
  - Service processes (All microservices)
  - Infrastructure processes (Saga orchestrator, event management)
  - Data processes (MongoDB, Redis, Event Store)
- **Use Case**: Understanding runtime behavior and process communication

#### 3. **Development View** (`03-development-view.puml`)
- **Purpose**: Shows development structure, modules, and dependencies
- **Scope**: Code organization and build dependencies
- **Key Elements**:
  - Client applications (Web, Mobile)
  - API Gateway
  - Microservices (organized by domain)
  - Infrastructure services (Saga management, event management)
  - Shared libraries and infrastructure
  - External dependencies
- **Use Case**: Understanding code organization and build dependencies

#### 4. **Physical View** (`04-physical-view.puml`)
- **Purpose**: Shows deployment architecture and infrastructure
- **Scope**: Hardware and deployment topology
- **Key Elements**:
  - Client devices
  - Load balancer tier
  - API Gateway tier
  - Microservices tier (with multiple instances)
  - Infrastructure services tier
  - Data tier (with replication)
  - Monitoring tier
- **Use Case**: Understanding deployment and infrastructure requirements

#### 5. **Use Case View** (`05-use-case-view.puml`) - The +1 View
- **Purpose**: Shows system functionality from user perspective
- **Scope**: Business functionality and user interactions
- **Key Elements**:
  - Actors (Customer, Store Manager, System Administrator, External systems)
  - Use cases organized by domain:
    - Product Management
    - Customer Management
    - Shopping Cart
    - Order Management
    - Sales & Analytics
    - System Administration
    - Saga Management
    - Event Management
- **Use Case**: Understanding business requirements and user interactions

### Domain-Specific Diagrams

#### 6. **Cart Class Diagram** (`06-cart-class-diagram.puml`)
- **Purpose**: Detailed class structure for cart functionality
- **Scope**: Cart domain and related classes
- **Key Elements**:
  - Cart and CartItem classes
  - CartService with dependencies
  - Product and Customer domain classes
  - Infrastructure interfaces
  - Controllers and DTOs
  - Exception classes
- **Use Case**: Understanding cart implementation and class relationships

#### 7. **Add Item to Cart Sequence** (`07-add-item-to-cart-sequence.puml`)
- **Purpose**: Detailed interaction flow for adding items to cart
- **Scope**: Complete request flow from user to database
- **Key Elements**:
  - User interaction flow
  - API Gateway processing
  - Service interactions
  - Validation steps
  - Cache operations
  - Database operations
  - Error handling
- **Use Case**: Understanding the complete flow of adding items to cart

## How to Use These Diagrams

### For Developers
1. **Start with Logical View**: Understand the overall system architecture
2. **Review Development View**: Understand code organization and dependencies
3. **Check Process View**: Understand runtime behavior
4. **Examine Physical View**: Understand deployment requirements
5. **Review Use Case View**: Understand business requirements

### For Architects
1. **Use Logical View**: For system design discussions
2. **Use Process View**: For performance and scalability analysis
3. **Use Physical View**: For infrastructure planning
4. **Use Development View**: For team organization and build planning

### For Business Analysts
1. **Focus on Use Case View**: Understand business functionality
2. **Review Sequence Diagrams**: Understand specific business processes
3. **Check Class Diagrams**: Understand data relationships

### For Operations Teams
1. **Use Physical View**: For deployment planning
2. **Use Process View**: For monitoring and troubleshooting
3. **Use Logical View**: For understanding system dependencies

## Generating Diagrams

### Prerequisites
- PlantUML installed or available online
- Java Runtime Environment (JRE)

### Local Generation
```bash
# Install PlantUML
java -jar plantuml.jar *.puml

# Or use PlantUML extension in VS Code
```

### Online Generation
1. Copy the PlantUML content
2. Visit http://www.plantuml.com/plantuml/
3. Paste the content and generate

### VS Code Integration
1. Install PlantUML extension
2. Open .puml files
3. Use Ctrl+Shift+P and select "PlantUML: Preview Current Diagram"

## Diagram Maintenance

### When to Update
- **Logical View**: When adding/removing services or changing architecture
- **Process View**: When changing service interactions or data flow
- **Development View**: When adding/removing projects or dependencies
- **Physical View**: When changing deployment topology
- **Use Case View**: When adding/removing business functionality
- **Class Diagrams**: When changing domain models or class relationships
- **Sequence Diagrams**: When changing business processes

### Best Practices
1. **Keep diagrams up to date** with code changes
2. **Use consistent naming** conventions across all diagrams
3. **Include relevant notes** for complex interactions
4. **Version control** diagrams with code
5. **Review diagrams** during code reviews

## Diagram Conventions

### Naming Conventions
- **Files**: `{number}-{view-name}.puml`
- **Components**: Use PascalCase for classes, camelCase for methods
- **Packages**: Use descriptive names in Title Case
- **Actors**: Use role-based names (Customer, Manager, etc.)

### Color Scheme
- **Background**: White (#FFFFFF)
- **Borders**: Blue (#2E86AB)
- **Fill**: Light Blue (#F0F8FF)
- **Use Cases**: Light Blue (#E8F4FD)

### Layout Guidelines
- **Logical View**: Organize by layers and domains
- **Process View**: Show flow from left to right
- **Development View**: Group by solution structure
- **Physical View**: Organize by deployment tiers
- **Use Case View**: Group by business domains

## Related Documentation

- **Technical Documentation**: `../TECHNICAL_DOCUMENTATION_COMPLETE.md`
- **Instruction Documentation**: `../INSTRUCTION_DOCUMENTATION_COMPLETE.md`
- **API Specification**: `../openapi-specification.json`
- **Architecture Decisions**: `../ADR/`

## Support

For questions about these diagrams:
1. Check the PlantUML documentation
2. Review the technical documentation
3. Consult with the development team
4. Update diagrams as the system evolves 