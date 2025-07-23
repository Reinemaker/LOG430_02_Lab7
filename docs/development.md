# Development Guide

## Project Setup

1. **Prerequisites**
   - .NET 8.0 or later
   - MongoDB
   - SQLite (included with EF Core)
   - IDE (Visual Studio, VS Code, etc.)

2. **Clone and Build**
   ```bash
   git clone [repository-url]
   cd CornerShop
   dotnet build
   ```

## Project Structure

```
CornerShop/
├── Controllers/
│   ├── Api/                    # REST API Controllers
│   │   ├── ProductsApiController.cs
│   │   ├── StoresApiController.cs
│   │   ├── SalesApiController.cs
│   │   └── ReportsApiController.cs
│   ├── HomeController.cs
│   ├── ProductController.cs
│   ├── SaleController.cs
│   └── StoreController.cs
├── Models/
│   ├── Product.cs
│   ├── Sale.cs
│   ├── SaleItem.cs
│   ├── Store.cs
│   └── ApiModels.cs           # API Response Models
├── Services/
│   ├── IDatabaseService.cs
│   ├── MongoDatabaseService.cs
│   ├── EfDatabaseService.cs
│   ├── IProductService.cs
│   ├── ProductService.cs
│   ├── ISaleService.cs
│   ├── SaleService.cs
│   ├── ISyncService.cs
│   ├── SyncService.cs
│   ├── CorsService.cs         # CORS Configuration
│   └── StoreService.cs
├── Views/
│   └── Home/
│       └── ApiDocumentation.cshtml
├── wwwroot/
│   └── cors-test.html         # CORS Testing Page
└── Program.cs
```

## REST API Architecture

### API Design Principles
- **REST Compliance**: Follow all REST principles and constraints
- **Versioning**: All endpoints use `/api/v1/` prefix
- **HATEOAS**: Include navigation links in all responses
- **Standardized Errors**: Consistent error format across all endpoints
- **Content Negotiation**: Support for JSON and XML formats

### API Response Format
All API responses follow a standardized format:

```csharp
public class ApiResponse<T>
{
    public T Data { get; set; }
    public List<Link> Links { get; set; }
    public DateTime Timestamp { get; set; }
}

public class Link
{
    public string Href { get; set; }
    public string Rel { get; set; }
    public string Method { get; set; }
}
```

### Error Response Format
Standardized error responses:

```csharp
public class ErrorResponse
{
    public DateTime Timestamp { get; set; }
    public int Status { get; set; }
    public string Error { get; set; }
    public string Message { get; set; }
    public string Path { get; set; }
}
```

## Adding New API Endpoints

1. **Create Controller**
   ```csharp
   [ApiController]
   [Route("api/v1/[controller]")]
   [Produces("application/json")]
   [EnableCors("ApiPolicy")]
   public class NewApiController : ControllerBase
   {
       // Implementation
   }
   ```

2. **Add HATEOAS Links**
   ```csharp
   var response = new ApiResponse<YourModel>
   {
       Data = data,
       Links = new List<Link>
       {
           new Link { Href = Url.Action(nameof(GetMethod)), Rel = "self", Method = "GET" },
           new Link { Href = Url.Action(nameof(CreateMethod)), Rel = "create", Method = "POST" }
       }
   };
   ```

3. **Handle Errors**
   ```csharp
   return BadRequest(new ErrorResponse
   {
       Timestamp = DateTime.UtcNow,
       Status = 400,
       Error = "Bad Request",
       Message = "Validation error message",
       Path = Request.Path
   });
   ```

4. **Add Caching**
   ```csharp
   [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
   public async Task<ActionResult<ApiResponse<T>>> GetMethod()
   ```

## Database Implementation

### MongoDB
- Uses MongoDB.Driver
- Document-based storage
- BSON serialization
- ObjectId for document IDs

### Entity Framework Core
- Uses Microsoft.EntityFrameworkCore.Sqlite
- Code-first approach
- Automatic migrations
- LINQ queries

### SQLite (Local Store Databases)
- Each store has its own SQLite database
- Offline operation support
- Automatic database creation on store creation

## CORS Configuration

### Development Environment
- Allows all origins, methods, and headers
- Configured in `CorsService.cs`

### Production Environment
- Restricted origins from `appsettings.json`
- Specific API policy for API endpoints

### Testing CORS
- Use the CORS test page at `/cors-test.html`
- Test cross-origin requests from different domains

## API Documentation

### Swagger/OpenAPI
- Automatic generation from controller attributes
- Available at `/api-docs`
- Interactive testing and documentation

### ReDoc
- Alternative documentation viewer
- Available at `/redoc`
- Clean, readable format

### Manual Documentation
- API documentation page at `/Home/ApiDocumentation`
- Comprehensive endpoint descriptions
- Examples and testing instructions

## Adding New Features

1. **Model Changes**
   - Update model classes
   - Add BSON attributes for MongoDB
   - Add data annotations for EF Core
   - Update both database services
   - Consider API response models

2. **Service Layer**
   - Update interfaces
   - Implement in both database services
   - Add synchronization logic
   - Update business logic
   - Ensure API compatibility

3. **API Layer**
   - Add new controller methods
   - Include HATEOAS links
   - Add proper error handling
   - Implement caching strategy
   - Update API documentation

4. **UI Changes**
   - Update MVC controllers
   - Add input validation
   - Improve user feedback
   - Handle errors gracefully
   - Update documentation pages

## Database Synchronization

### SyncService Implementation
- Bidirectional sync
- Conflict resolution
- Error handling
- Transaction management

### Sync Points
- After critical operations
- On database switch
- Manual sync
- Error recovery

## Testing

1. **Unit Tests**
   - Test business logic
   - Mock database services
   - Verify sync operations
   - Test error handling
   - Test API responses

2. **Integration Tests**
   - Test database operations
   - Verify sync functionality
   - Test error recovery
   - Performance testing
   - API endpoint testing

3. **API Testing**
   - Use Swagger UI for interactive testing
   - Test CORS functionality
   - Verify HATEOAS links
   - Test error responses
   - Validate content negotiation

## Best Practices

1. **Code Organization**
   - Follow SOLID principles
   - Use dependency injection
   - Maintain separation of concerns
   - Document public APIs
   - Keep API controllers thin

2. **API Design**
   - Follow REST principles strictly
   - Use appropriate HTTP methods
   - Include HATEOAS links
   - Standardize error responses
   - Implement proper caching

3. **Database Operations**
   - Use transactions where appropriate
   - Handle errors gracefully
   - Implement retry logic
   - Log important operations
   - Consider offline scenarios

4. **Synchronization**
   - Keep sync logic simple
   - Handle conflicts properly
   - Log sync operations
   - Monitor sync performance
   - Ensure data consistency

5. **Security**
   - Validate all inputs
   - Implement proper CORS policies
   - Consider authentication for production
   - Log security events
   - Monitor API usage

## Deployment

1. **Requirements**
   - .NET 8.0 runtime
   - MongoDB server
   - SQLite (included)
   - Web server (IIS, Nginx, etc.)

2. **Configuration**
   - Update connection strings
   - Set sync preferences
   - Configure logging
   - Set error handling
   - Configure CORS policies

3. **API Deployment**
   - Ensure HTTPS in production
   - Configure proper CORS origins
   - Set up API monitoring
   - Implement rate limiting
   - Configure caching headers

4. **Monitoring**
   - Monitor database sync
   - Check error logs
   - Track performance
   - Verify data integrity
   - Monitor API usage and errors

## API Versioning Strategy

### Current Version: v1
- All endpoints use `/api/v1/` prefix
- Stable API with backward compatibility
- Comprehensive documentation

### Future Versions
- Use `/api/v2/`, `/api/v3/`, etc.
- Maintain backward compatibility within major versions
- Deprecate old versions gradually
- Provide migration guides

## Development Workflow

1. **Feature Development**
   - Create feature branch
   - Implement API endpoints
   - Add HATEOAS links
   - Include error handling
   - Update documentation

2. **Testing**
   - Unit tests for business logic
   - Integration tests for API
   - CORS testing
   - Performance testing

3. **Documentation**
   - Update API documentation
   - Add examples
   - Update README files
   - Create migration guides if needed

4. **Deployment**
   - Test in staging environment
   - Verify CORS configuration
   - Monitor API performance
   - Update production documentation 