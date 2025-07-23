# Corner Shop - Technical Documentation

## Overview
Corner Shop is a distributed, web-based multi-store management system with a full REST API. Each store operates with its own local SQLite database for products and sales, supporting full offline operation. The head office uses a central MongoDB database for consolidated reporting and administration. A sync service allows admins to push all unsynced sales from local SQLite to MongoDB with a single click.

## Architecture
- **Web Client**: ASP.NET Core MVC
- **REST API**: ASP.NET Core Web API with full REST compliance
- **Store Data Layer**: Local SQLite per store
- **Central Data Layer**: MongoDB
- **Sync Service**: Handles pushing local sales to MongoDB

## Key Features
- Local, offline operation for each store
- Centralized, consolidated reporting for the head office
- One-click sync from the Reports page
- Modern, responsive web interface
- **Full REST API** with versioning, HATEOAS, and standardized error handling
- **Cross-Origin Support** for frontend integration
- **OpenAPI 3.0 Documentation** with Swagger UI and ReDoc

## REST API Compliance
The API follows all REST principles and constraints:

### REST Principles
- **Stateless**: Each request contains all necessary information
- **Cacheable**: Responses include appropriate cache headers
- **Uniform Interface**: Standard HTTP methods and URI patterns
- **Layered System**: API abstracts underlying complexity
- **HATEOAS**: Hypermedia links in all responses

### API Features
- **Versioning**: All endpoints use `/api/v1/` prefix
- **HATEOAS**: Navigation links in all responses
- **Standardized Errors**: Consistent error format with timestamp, status, and path
- **HTTP Status Codes**: Proper use of 200, 201, 204, 400, 404, 500
- **Caching**: Response caching with appropriate durations
- **Content Negotiation**: Support for JSON and XML formats
- **PATCH Support**: Partial updates for all resources

## Usage
- On store creation, a new SQLite file is created for that store
- All store operations use the local SQLite file
- Use the "Sync All Stores" button on the Reports page to push all unsynced sales to MongoDB
- API endpoints are available at `/api/v1/` for programmatic access

## API Documentation
- **Swagger UI**: [http://localhost:5000/api-docs](http://localhost:5000/api-docs)
- **ReDoc UI**: [http://localhost:5000/redoc](http://localhost:5000/redoc)
- **API Documentation Page**: [http://localhost:5000/Home/ApiDocumentation](http://localhost:5000/Home/ApiDocumentation)

## See Also
- [UML Diagrams](UML/)
- [ADR: Database Architecture](ADR/002-database-architecture.md)
- [API Documentation](API_README.md)
- [CORS Configuration](CORS_README.md)

## Table of Contents
1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture Decisions](#architecture-decisions)
4. [System Design](#system-design)
5. [REST API Design](#rest-api-design)
6. [Development Setup](#development-setup)
7. [Testing](#testing)
8. [Deployment](#deployment)

## Project Overview
Corner Shop is a web-based point-of-sale system designed for multi-store retail businesses. It provides essential features for inventory management, sales processing, and comprehensive reporting with full REST API support.

### Key Features
- Product inventory management
- Sales processing and tracking
- Multi-store support with local databases
- Centralized reporting and administration
- Full REST API with OpenAPI documentation
- Cross-origin resource sharing (CORS) support

## Technology Stack

### Core Technologies
- **.NET 8.0**: Chosen for its robust performance, cross-platform capabilities, and modern C# features
- **ASP.NET Core MVC**: Web framework for the user interface
- **ASP.NET Core Web API**: REST API framework with full compliance
- **MongoDB**: Selected for its flexibility with document-based storage and ease of scaling
- **SQLite**: Local database for each store's offline operations
- **Docker**: Used for containerization to ensure consistent deployment across environments

### Development Tools
- **xUnit**: For unit testing
- **Moq**: For mocking dependencies in tests
- **dotnet-format**: For code formatting and style consistency
- **Swagger/OpenAPI**: For API documentation and testing

### Justification for Technology Choices

#### .NET 8.0
- Modern, cross-platform framework
- Strong typing and compile-time checks
- Excellent performance characteristics
- Rich ecosystem of libraries and tools

#### ASP.NET Core Web API
- Built-in support for REST principles
- Automatic OpenAPI/Swagger generation
- Content negotiation support
- Comprehensive middleware pipeline

#### MongoDB
- Flexible schema design
- Easy to scale horizontally
- Excellent performance for read/write operations
- Native support for JSON-like documents

#### SQLite
- Lightweight, serverless database
- Perfect for local store operations
- Zero-configuration setup
- Excellent offline support

#### Docker
- Consistent development and deployment environments
- Easy dependency management
- Simplified deployment process
- Isolation of services

## REST API Design

### API Structure
```
/api/v1/
├── products/          # Product management
├── stores/           # Store management
├── sales/            # Sales operations
└── reports/          # Reporting and analytics
```

### Response Format
All API responses follow a standardized format with HATEOAS links:

```json
{
  "data": { /* actual response data */ },
  "links": [
    {
      "href": "/api/v1/products",
      "rel": "self",
      "method": "GET"
    }
  ],
  "timestamp": "2025-01-27T10:30:00Z"
}
```

### Error Response Format
Standardized error responses for all endpoints:

```json
{
  "timestamp": "2025-01-27T10:30:00Z",
  "status": 400,
  "error": "Bad Request",
  "message": "Search term is required",
  "path": "/api/v1/products/search"
}
```

### Content Negotiation
The API supports multiple content types:
- `application/json` (default)
- `application/xml` (via Accept header)

### Caching Strategy
- **Products/Stores**: 5 minutes cache
- **Sales**: 1-5 minutes cache (depending on endpoint)
- **Reports**: 5-10 minutes cache
- **Search results**: 1 minute cache

## Development Setup

### Prerequisites
- .NET 8.0 SDK
- Docker and Docker Compose
- MongoDB (if running locally without Docker)

### Local Development
1. Clone the repository
2. Navigate to the project directory
3. Run `dotnet restore` to restore dependencies
4. Run `dotnet build` to build the project
5. Run `dotnet test` to execute tests
6. Run `dotnet run` to start the application

### Docker Development
1. Build the Docker image:
   ```bash
   docker build -t cornershop .
   ```
2. Run with Docker Compose:
   ```bash
   docker-compose up
   ```

## Testing

### Unit Tests
- Located in `CornerShop.Tests/`
- Run tests using:
  ```bash
  dotnet test
  ```

### API Tests
- Test API endpoints using Swagger UI at `/api-docs`
- Use the CORS test page at `/cors-test.html` for cross-origin testing

### Test Coverage
- Using Coverlet for code coverage
- Generate coverage report:
  ```bash
  dotnet test /p:CollectCoverage=true
  ```

## Deployment

### Docker Deployment
1. Build the image:
   ```bash
   docker build -t cornershop .
   ```
2. Run the container:
   ```bash
   docker run -p 5000:5000 -p 27017:27017 cornershop
   ```

### Docker Compose Deployment
1. Start all services:
   ```bash
   docker-compose up -d
   ```
2. Stop all services:
   ```bash
   docker-compose down
   ```

## System Architecture
See the [UML diagrams](UML/) for detailed system architecture views.

## Architecture Decisions
See the [Architecture Decision Records](ADR/) for detailed documentation of key architectural decisions.

## New Features
- Each store uses a local SQLite database for products and sales
- Admins can sync all stores' local data to the central MongoDB from the Reports page
- Offline operation is supported; sync when online
- Full REST API with OpenAPI 3.0 documentation
- HATEOAS support for navigation
- Standardized error handling
- CORS support for frontend integration
- Content negotiation for multiple formats
- PATCH support for partial updates

## System Design (Updated)
- Local SQLite per store for fast, offline operations
- Central MongoDB for consolidated reporting and administration
- Sync service to push local sales to MongoDB
- REST API layer for programmatic access
- CORS support for cross-origin requests
- Comprehensive API documentation

## Usage
- On store creation, a new SQLite file is created
- Use the "Sync All Stores" button on the Reports page to push all unsynced sales to MongoDB
- API endpoints are available for integration with other systems
- Swagger UI provides interactive API testing and documentation

## See Also
- [UML Diagrams](UML/) (updated for sync and local DB)
- [ADR: Database Architecture](ADR/002-database-architecture.md)
- [API Documentation](API_README.md)
- [CORS Configuration](CORS_README.md) 