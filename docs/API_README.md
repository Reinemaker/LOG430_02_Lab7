# CornerShop REST API Documentation

## Overview

The CornerShop REST API provides a comprehensive interface for managing stores, products, sales, and reports. The API follows REST principles and includes authentication, pagination, filtering, sorting, and comprehensive error handling.

## Base URL

```
https://localhost:5001/api/v1
```

## Authentication

The API uses JWT Bearer authentication for secured endpoints. Public endpoints (GET operations for products and stores) do not require authentication.

### Login

**POST** `/auth/login`

Get a JWT token for authentication.

**Request Body:**
```json
{
  "username": "admin",
  "password": "password"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Usage:**
```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "password"}'
```

### Using Authentication

Include the JWT token in the Authorization header:

```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  https://localhost:5001/api/v1/products
```

## Common Features

### Pagination

Most collection endpoints support pagination with the following query parameters:

- `page` (default: 1) - Page number
- `pageSize` (default: 20) - Number of items per page
- `sortBy` - Field to sort by
- `sortOrder` - Sort order: `asc` or `desc`

**Example:**
```bash
GET /api/v1/products?page=1&pageSize=10&sortBy=Name&sortOrder=asc
```

### Filtering

Some endpoints support filtering with `searchTerm` parameter:

```bash
GET /api/v1/products?searchTerm=bread&page=1&pageSize=5
```

### Error Responses

All endpoints return standardized error responses:

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "status": 400,
  "error": "Bad Request",
  "message": "Validation error details",
  "path": "/api/v1/products"
}
```

### HATEOAS Links

All responses include HATEOAS links for navigation:

```json
{
  "data": [...],
  "links": [
    {
      "href": "/api/v1/products",
      "rel": "self",
      "method": "GET"
    },
    {
      "href": "/api/v1/products",
      "rel": "create",
      "method": "POST"
    }
  ]
}
```

## Endpoints

### Products

#### Get All Products
**GET** `/products`

**Query Parameters:**
- `page` (optional) - Page number
- `pageSize` (optional) - Items per page
- `sortBy` (optional) - Sort field (Name, Category, Price)
- `sortOrder` (optional) - Sort order (asc, desc)
- `searchTerm` (optional) - Filter by name or category

**Example:**
```bash
curl "https://localhost:5001/api/v1/products?page=1&pageSize=10&sortBy=Name&sortOrder=asc"
```

#### Get Product by ID
**GET** `/products/{id}`

**Example:**
```bash
curl https://localhost:5001/api/v1/products/123
```

#### Create Product
**POST** `/products` (Requires Authentication)

**Request Body:**
```json
{
  "name": "Bread",
  "category": "Bakery",
  "price": 2.99,
  "stockQuantity": 50,
  "minimumStockLevel": 10,
  "reorderPoint": 5
}
```

#### Update Product
**PUT** `/products/{id}` (Requires Authentication)

#### Patch Product
**PATCH** `/products/{id}` (Requires Authentication)

**Request Body:**
```json
{
  "name": "Updated Bread",
  "price": 3.49
}
```

#### Delete Product
**DELETE** `/products/{id}` (Requires Authentication)

#### Search Products
**GET** `/products/search`

**Query Parameters:**
- `searchTerm` (required) - Search term
- `storeId` (optional) - Filter by store
- `page`, `pageSize`, `sortBy`, `sortOrder` (optional) - Pagination and sorting

### Stores

#### Get All Stores
**GET** `/stores`

**Query Parameters:**
- `page`, `pageSize`, `sortBy`, `sortOrder`, `searchTerm` (optional)

#### Get Store by ID
**GET** `/stores/{id}`

#### Create Store
**POST** `/stores` (Requires Authentication)

**Request Body:**
```json
{
  "name": "Downtown Store",
  "location": "Downtown",
  "address": "123 Main St",
  "isHeadquarters": false,
  "status": "Active"
}
```

#### Update Store
**PUT** `/stores/{id}` (Requires Authentication)

#### Patch Store
**PATCH** `/stores/{id}` (Requires Authentication)

#### Delete Store
**DELETE** `/stores/{id}` (Requires Authentication)

#### Search Stores
**GET** `/stores/search`

### Sales

#### Get Recent Sales
**GET** `/sales/store/{storeId}/recent` (Requires Authentication)

**Query Parameters:**
- `limit` (optional) - Number of recent sales
- `page`, `pageSize`, `sortBy`, `sortOrder` (optional)

#### Get Sale by ID
**GET** `/sales/{id}` (Requires Authentication)

#### Get Sale Details
**GET** `/sales/{id}/details` (Requires Authentication)

#### Create Sale
**POST** `/sales` (Requires Authentication)

**Request Body:**
```json
{
  "storeId": "store123",
  "items": [
    {
      "productName": "Bread",
      "quantity": 2,
      "unitPrice": 2.99
    }
  ]
}
```

#### Cancel Sale
**POST** `/sales/{id}/cancel` (Requires Authentication)

**Query Parameters:**
- `storeId` (required) - Store ID

#### Get Sales by Date Range
**GET** `/sales/date-range` (Requires Authentication)

**Query Parameters:**
- `startDate` (required) - Start date
- `endDate` (required) - End date
- `storeId` (optional) - Filter by store
- `page`, `pageSize`, `sortBy`, `sortOrder` (optional)

### Reports

#### Consolidated Sales Report
**GET** `/reports/sales/consolidated` (Requires Authentication)

**Query Parameters:**
- `startDate` (optional) - Start date
- `endDate` (optional) - End date

#### Inventory Report
**GET** `/reports/inventory` (Requires Authentication)

#### Top Selling Products
**GET** `/reports/products/top-selling` (Requires Authentication)

**Query Parameters:**
- `limit` (optional) - Number of products (default: 10)
- `storeId` (optional) - Filter by store

#### Sales Trend Report
**GET** `/reports/sales/trend` (Requires Authentication)

**Query Parameters:**
- `period` (optional) - Period grouping (daily, weekly, monthly)
- `startDate` (optional) - Start date
- `endDate` (optional) - End date

## HTTP Status Codes

- **200 OK** - Request successful
- **201 Created** - Resource created successfully
- **204 No Content** - Request successful, no content returned
- **400 Bad Request** - Invalid request data
- **401 Unauthorized** - Authentication required
- **404 Not Found** - Resource not found
- **500 Internal Server Error** - Server error

## Architecture

The API layer is built on top of the existing MVC architecture:

```
┌─────────────────┐
│   API Layer     │  ← REST API Controllers with Authentication
├─────────────────┤
│   MVC Layer     │  ← Existing MVC Controllers
├─────────────────┤
│  Service Layer  │  ← Shared Business Logic
├─────────────────┤
│  Data Layer     │  ← Database Services
└─────────────────┘
```

### Key Features

1. **Authentication & Authorization**: JWT Bearer token authentication
2. **Pagination & Sorting**: Built-in support for large datasets
3. **Filtering**: Search capabilities across collections
4. **HATEOAS**: Hypermedia links for API navigation
5. **Error Handling**: Standardized error responses
6. **CORS Support**: Cross-origin request handling
7. **Caching**: Response caching for performance
8. **Content Negotiation**: Support for JSON and XML formats

## Development

### Adding New API Endpoints

1. Create a new API controller in `Controllers/Api/`
2. Inherit from `ControllerBase`
3. Use the `[ApiController]` attribute
4. Add `[Authorize]` for secured endpoints or `[AllowAnonymous]` for public endpoints
5. Add XML documentation comments for Swagger
6. Use appropriate HTTP status codes and response types
7. Include pagination, sorting, and filtering parameters for collection endpoints

### Testing the API

1. **Manual Testing**: Use the Swagger UI at `/api-docs`
2. **Automated Testing**: Run integration tests in the test project
3. **Tools**: Use Postman, curl, or other HTTP clients

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ApiIntegrationTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Security Considerations

The API implements the following security measures:

- **JWT Authentication**: Secure token-based authentication
- **Authorization**: Role-based access control (can be extended)
- **Input Validation**: Comprehensive request validation
- **CORS Configuration**: Controlled cross-origin access
- **HTTPS Enforcement**: Secure communication in production

For production use, consider additional measures:

- Rate limiting
- API key management
- Request logging and monitoring
- Input sanitization
- SQL injection prevention

## Contributing

When adding new API endpoints:

1. Follow REST conventions
2. Add comprehensive XML documentation
3. Include appropriate response types
4. Add error handling
5. Implement pagination for collection endpoints
6. Add integration tests
7. Update this documentation

## Support

For API support and questions:

- Check the Swagger documentation at `/api-docs`
- Review the integration tests for usage examples
- Consult the development documentation in `/docs/` 