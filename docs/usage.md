# Usage Guide

## Starting the Application

1. Navigate to the project directory
2. Run the application:
   ```bash
   dotnet run
   ```
3. The application will start on `http://localhost:5000`
4. Choose your preferred database:
   - MongoDB (central database)
   - SQLite (local store databases)

## Web Interface

### Main Dashboard
- **Home**: Overview and navigation
- **Products**: Manage product inventory
- **Sales**: Process sales and view history
- **Stores**: Manage store locations
- **Reports**: View consolidated reports

### Product Management
1. **View Products**: Browse all products across stores
2. **Add Product**: Create new products with details
3. **Edit Product**: Update product information
4. **Search Products**: Find products by name or category
5. **Low Stock Alerts**: View products needing restocking

### Sales Processing
1. **Create Sale**: Add items to cart and process sale
2. **View Sales**: Browse recent sales by store
3. **Sale Details**: View detailed sale information
4. **Cancel Sale**: Cancel existing sales if needed

### Store Management
1. **View Stores**: List all store locations
2. **Add Store**: Create new store locations
3. **Edit Store**: Update store information
4. **Store Status**: Monitor store status and operations

### Reporting
1. **Consolidated Reports**: View sales across all stores
2. **Inventory Reports**: Check stock levels and values
3. **Top Products**: See best-selling items
4. **Sales Trends**: Analyze sales patterns over time

## REST API Usage

### API Access
- **Base URL**: `http://localhost:5000/api/v1/`
- **Documentation**: `http://localhost:5000/api-docs`
- **Alternative Docs**: `http://localhost:5000/redoc`
- **API Guide**: `http://localhost:5000/Home/ApiDocumentation`

### Authentication
Currently, no authentication is required. All endpoints are publicly accessible.

### Content Types
The API supports multiple formats:
- **JSON** (default): `application/json`
- **XML**: `application/xml` (via Accept header)

### Example API Calls

#### Get All Products
```bash
curl http://localhost:5000/api/v1/products
```

#### Create a Product
```bash
curl -X POST http://localhost:5000/api/v1/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop",
    "category": "Electronics",
    "price": 999.99,
    "storeId": "store-123",
    "stockQuantity": 10,
    "minimumStockLevel": 2,
    "reorderPoint": 1
  }'
```

#### Search Products
```bash
curl "http://localhost:5000/api/v1/products/search?searchTerm=laptop"
```

#### Get Products by Store
```bash
curl http://localhost:5000/api/v1/products/store/store-123
```

#### Create a Sale
```bash
curl -X POST http://localhost:5000/api/v1/sales \
  -H "Content-Type: application/json" \
  -d '{
    "storeId": "store-123",
    "items": [
      {
        "productName": "Laptop",
        "quantity": 1,
        "unitPrice": 999.99
      }
    ]
  }'
```

#### Get Sales Report
```bash
curl "http://localhost:5000/api/v1/reports/sales/consolidated?startDate=2025-01-01&endDate=2025-01-31"
```

### API Response Format
All responses include HATEOAS links for navigation:

```json
{
  "data": { /* actual response data */ },
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
  ],
  "timestamp": "2025-01-27T10:30:00Z"
}
```

### Error Handling
API errors follow a standardized format:

```json
{
  "timestamp": "2025-01-27T10:30:00Z",
  "status": 400,
  "error": "Bad Request",
  "message": "Search term is required",
  "path": "/api/v1/products/search"
}
```

## CORS Testing

### Test Cross-Origin Requests
1. Open `http://localhost:5000/cors-test.html`
2. Test API calls from different origins
3. Verify CORS headers are properly set
4. Test different HTTP methods

### CORS Configuration
- **Development**: Allows all origins
- **Production**: Restricted to configured origins
- **API Policy**: Specific policy for API endpoints

## Database Synchronization

### Automatic Sync
The system automatically synchronizes data:
- After creating a sale
- After canceling a sale
- When switching databases
- On manual sync request

### Manual Sync
1. Navigate to Reports page
2. Click "Sync All Stores" button
3. Monitor sync progress
4. Verify data consistency

### Sync Status
- Check sync status in the UI
- Monitor sync logs
- Verify data integrity
- Handle sync conflicts

## Error Handling

### Web Interface Errors
- Clear error messages for invalid input
- Database error notifications
- Stock validation warnings
- Sale operation confirmations

### API Errors
- Standardized error responses
- HTTP status codes
- Detailed error messages
- Request path information

### Common Issues
1. **Product Not Found**
   - Check spelling and case
   - Verify database sync
   - Search across all stores

2. **Sale Errors**
   - Verify store ID
   - Check stock levels
   - Ensure database sync

3. **API Errors**
   - Check request format
   - Verify endpoint URL
   - Review error response

## Best Practices

### Web Interface
1. **Creating Sales**
   - Verify product names
   - Check stock levels
   - Review sale summary
   - Confirm before processing

2. **Managing Products**
   - Keep product names consistent
   - Set appropriate stock levels
   - Update prices regularly
   - Monitor low stock alerts

3. **Store Management**
   - Keep store information current
   - Monitor store status
   - Regular data synchronization

### API Usage
1. **Request Formatting**
   - Use proper HTTP methods
   - Include required headers
   - Format JSON correctly
   - Handle responses properly

2. **Error Handling**
   - Check HTTP status codes
   - Parse error responses
   - Implement retry logic
   - Log error details

3. **Performance**
   - Use caching headers
   - Implement pagination
   - Monitor response times
   - Optimize requests

### Database Management
1. **Regular Maintenance**
   - Monitor sync status
   - Backup important data
   - Check data integrity
   - Review error logs

2. **Synchronization**
   - Regular manual syncs
   - Monitor automatic syncs
   - Handle sync conflicts
   - Verify data consistency

## Troubleshooting

### Web Interface Issues
1. **Page Not Loading**
   - Check server status
   - Verify port configuration
   - Clear browser cache
   - Check network connectivity

2. **Data Not Updating**
   - Refresh the page
   - Check database sync
   - Verify store selection
   - Monitor error logs

### API Issues
1. **Connection Errors**
   - Verify API endpoint
   - Check server status
   - Test with curl/Postman
   - Review CORS configuration

2. **Authentication Errors**
   - Check API documentation
   - Verify request format
   - Review error responses
   - Test with different tools

### Database Issues
1. **Sync Problems**
   - Check database connections
   - Verify sync configuration
   - Review sync logs
   - Manual sync if needed

2. **Data Inconsistencies**
   - Compare databases
   - Check sync timestamps
   - Review error messages
   - Restore from backup if needed

## Monitoring and Logs

### Application Logs
- Monitor application startup
- Check database connections
- Review sync operations
- Track API usage

### Performance Monitoring
- Monitor response times
- Track database performance
- Check memory usage
- Monitor disk space

### Security Monitoring
- Monitor API access
- Check CORS violations
- Review error patterns
- Track authentication attempts 