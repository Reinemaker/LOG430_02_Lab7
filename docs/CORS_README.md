# CORS Configuration - CornerShop API

This document explains the CORS (Cross-Origin Resource Sharing) configuration implemented in the CornerShop API.

## Overview

CORS is enabled to allow cross-origin requests from web applications running on different domains or ports. This is essential for:
- Frontend applications (React, Angular, Vue.js) consuming the API
- Mobile applications making API calls
- Third-party integrations

## Configuration

### 1. CORS Policies

The application implements three CORS policies:

#### **AllowAll Policy** (Development)
- **Use Case**: Development environment
- **Configuration**: Allows all origins, methods, and headers
- **Security**: Less restrictive, suitable for development only

#### **AllowSpecificOrigins Policy** (Production)
- **Use Case**: Production environment
- **Configuration**: Allows only specified origins from `appsettings.json`
- **Security**: More restrictive, production-ready

#### **ApiPolicy** (API Endpoints)
- **Use Case**: Specific API endpoints
- **Configuration**: Restricted origins with specific methods and headers
- **Security**: Most restrictive, for sensitive operations

### 2. Environment-Based Configuration

The CORS policy is automatically selected based on the environment:

```csharp
if (environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowSpecificOrigins");
}
```

### 3. Configuration File

CORS settings are configured in `appsettings.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:4200", 
      "http://localhost:8080",
      "https://localhost:3000",
      "https://localhost:4200",
      "https://localhost:8080"
    ],
    "AllowCredentials": true
  }
}
```

## Allowed Origins

### Development Servers
- `http://localhost:3000` - React development server
- `http://localhost:4200` - Angular development server
- `http://localhost:8080` - Vue.js development server
- HTTPS versions of the above

### Production
Add your production domains to the `AllowedOrigins` array in `appsettings.json`.

## Usage Examples

### 1. JavaScript/Fetch API

```javascript
// Get all products
fetch('http://localhost:5000/api/products')
  .then(response => response.json())
  .then(data => console.log(data));

// Create a product
fetch('http://localhost:5000/api/products', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    name: 'New Product',
    category: 'Electronics',
    price: 99.99
  })
})
.then(response => response.json())
.then(data => console.log(data));
```

### 2. Axios

```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Get all products
const products = await api.get('/products');

// Create a product
const newProduct = await api.post('/products', {
  name: 'New Product',
  category: 'Electronics',
  price: 99.99
});
```

### 3. Angular HttpClient

```typescript
import { HttpClient } from '@angular/common/http';

constructor(private http: HttpClient) {}

// Get all products
getProducts() {
  return this.http.get<any[]>('http://localhost:5000/api/products');
}

// Create a product
createProduct(product: any) {
  return this.http.post<any>('http://localhost:5000/api/products', product);
}
```

## Testing CORS

### 1. Built-in Test Page
Visit `/cors-test.html` to test CORS functionality with a simple web interface.

### 2. Browser Developer Tools
1. Open browser developer tools (F12)
2. Go to the Network tab
3. Make a request to the API
4. Check for CORS headers in the response

### 3. Command Line Testing

```bash
# Test CORS preflight request
curl -X OPTIONS http://localhost:5000/api/products \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type" \
  -v

# Test actual request
curl -X GET http://localhost:5000/api/products \
  -H "Origin: http://localhost:3000" \
  -v
```

## Security Considerations

### Development
- Uses permissive CORS policy
- Allows all origins, methods, and headers
- Suitable for local development only

### Production
- Uses restrictive CORS policy
- Only allows specified origins
- Configured through `appsettings.json`
- Consider using HTTPS in production

### Best Practices
1. **Never use `AllowAll` in production**
2. **Specify exact origins instead of wildcards**
3. **Use HTTPS in production**
4. **Regularly review and update allowed origins**
5. **Monitor CORS errors in logs**

## Troubleshooting

### Common CORS Errors

#### 1. "No 'Access-Control-Allow-Origin' header"
- Check if the origin is in the allowed origins list
- Verify CORS middleware is configured correctly

#### 2. "Method not allowed"
- Check if the HTTP method is allowed in the CORS policy
- Verify the endpoint supports the requested method

#### 3. "Headers not allowed"
- Check if the requested headers are allowed
- Add custom headers to the CORS policy if needed

### Debugging Steps
1. Check browser console for CORS errors
2. Verify the request origin matches allowed origins
3. Check server logs for CORS-related errors
4. Test with the built-in CORS test page
5. Verify environment configuration

## Configuration Files

- `Program.cs` - CORS service configuration
- `Services/CorsService.cs` - CORS policy definitions
- `appsettings.json` - CORS settings
- `wwwroot/cors-test.html` - CORS testing page

## API Endpoints with CORS

All API endpoints support CORS:
- `/api/products/*` - Product management
- `/api/stores/*` - Store management
- `/api/sales/*` - Sales management
- `/api/reports/*` - Reporting endpoints

## Support

For CORS-related issues:
1. Check the browser console for error messages
2. Review the CORS configuration in `appsettings.json`
3. Test with the built-in CORS test page
4. Check server logs for detailed error information 