# Documentation and Testing Guide

This guide explains how to access and use the documentation and testing files created for the CornerShop microservices project.

## 📁 File Locations

### Documentation Files
- **OpenAPI Specification**: `docs/openapi-specification.json`
- **API Documentation**: `docs/API_README.md`
- **Technical Documentation**: `docs/README-Technical.md`
- **Microservices Documentation**: `docs/README-Microservices.md`

### Testing Files
- **Postman Collection**: `postman/CornerShop-API.postman_collection.json`
- **API Gateway Test Script**: `test-gateway-functionality.sh`
- **Existing Test Scripts**: 
  - `test-api-gateway.sh`
  - `test-security-features.sh`
  - `test-load-balancing.sh`

## 🔍 How to Access the Documentation

### 1. OpenAPI/Swagger Documentation

#### View in Browser
```bash
# If you have a Swagger UI server running
# Navigate to: http://localhost:8080/swagger-ui/index.html
# And load the file: docs/openapi-specification.json
```

#### View Raw JSON
```bash
# View the OpenAPI specification
cat docs/openapi-specification.json

# Or use a JSON formatter for better readability
cat docs/openapi-specification.json | jq '.'
```

#### Online Swagger Editor
1. Go to [Swagger Editor](https://editor.swagger.io/)
2. Copy the contents of `docs/openapi-specification.json`
3. Paste it into the editor to view the interactive documentation

### 2. Postman Collection

#### Import into Postman
1. Open Postman
2. Click "Import" button
3. Select "File" tab
4. Choose `postman/CornerShop-API.postman_collection.json`
5. Click "Import"

#### Collection Features
- **Pre-configured Variables**: API key, base URL, and dynamic IDs
- **Authentication**: Automatic API key injection
- **Test Scripts**: Built-in validation and response testing
- **Environment Setup**: Ready-to-use collection variables

#### Using the Collection
1. **Set Environment Variables**:
   - `baseUrl`: `http://api.cornershop.localhost`
   - `apiKey`: `cornershop-api-key-2024`

2. **Run Tests**:
   - Start with "Health Check" to verify connectivity
   - Run "Authentication Tests" to verify security
   - Execute individual service tests

3. **Dynamic Variables**:
   - The collection automatically stores IDs from responses
   - Use these for subsequent requests (e.g., customer ID for cart operations)

## 🧪 How to Run the Tests

### 1. API Gateway Functionality Test

#### Prerequisites
```bash
# Make sure the script is executable
chmod +x test-gateway-functionality.sh

# Ensure your microservices are running
./start-microservices.sh
```

#### Run the Test
```bash
# Run the comprehensive gateway test
./test-gateway-functionality.sh
```

#### What the Test Covers
- ✅ Health check endpoint
- ✅ Authentication (API key validation)
- ✅ All service endpoints (Products, Customers, Cart, Orders)
- ✅ Error handling (404, 401, 403)
- ✅ Rate limiting
- ✅ Gateway headers
- ✅ CORS configuration

### 2. Existing Test Scripts

#### API Gateway Basic Test
```bash
./test-api-gateway.sh
```

#### Security Features Test
```bash
./test-security-features.sh
```

#### Load Balancing Test
```bash
./test-load-balancing.sh
```

## 📊 Understanding the Documentation

### OpenAPI Specification Structure

The `openapi-specification.json` includes:

1. **Info Section**: API metadata, version, contact information
2. **Servers**: Available server URLs (production and development)
3. **Security**: API key authentication scheme
4. **Paths**: All available endpoints organized by service
5. **Components**: Reusable schemas for requests/responses
6. **Tags**: Logical grouping of endpoints

### Key Endpoints Documented

#### Health Check
- `GET /health` - Gateway health status

#### Products Service
- `GET /api/products` - List all products
- `GET /api/products/{id}` - Get specific product
- `POST /api/products` - Create new product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product

#### Customers Service
- `GET /api/customers` - List all customers
- `GET /api/customers/{id}` - Get specific customer
- `POST /api/customers` - Create new customer

#### Cart Service
- `GET /api/cart` - Get customer's cart
- `POST /api/cart` - Add item to cart
- `DELETE /api/cart/{customerId}/items/{itemId}` - Remove item

#### Orders Service
- `GET /api/orders` - List all orders
- `GET /api/orders/{id}` - Get specific order
- `POST /api/orders` - Create new order
- `PUT /api/orders/{id}` - Update order status

### Authentication

All API endpoints require authentication via API key:
- **Header**: `X-API-Key`
- **Value**: `cornershop-api-key-2024`
- **Error Codes**: 
  - `401` - Missing API key
  - `403` - Invalid API key

## 🔧 Troubleshooting

### Common Issues

#### 1. Gateway Not Accessible
```bash
# Check if services are running
docker ps

# Check gateway logs
docker logs api-gateway

# Verify DNS resolution
nslookup api.cornershop.localhost
```

#### 2. Authentication Failures
```bash
# Verify API key is correct
echo "cornershop-api-key-2024"

# Test with curl
curl -H "X-API-Key: cornershop-api-key-2024" \
     http://api.cornershop.localhost/health
```

#### 3. Postman Import Issues
- Ensure the JSON file is valid
- Check Postman version compatibility
- Try importing as raw text if file import fails

#### 4. Test Script Failures
```bash
# Check if curl is installed
which curl

# Test basic connectivity
curl -I http://api.cornershop.localhost/health

# Check script permissions
ls -la test-gateway-functionality.sh
```

## 📈 Monitoring and Validation

### Success Indicators

1. **API Gateway Tests**: All tests pass with green checkmarks
2. **Postman Collection**: All requests return expected status codes
3. **OpenAPI Documentation**: All endpoints are properly documented
4. **Authentication**: Proper 401/403 responses for invalid requests

### Validation Checklist

- [ ] OpenAPI specification is up-to-date
- [ ] Postman collection includes all endpoints
- [ ] API Gateway routes all requests correctly
- [ ] Authentication is enforced on all endpoints
- [ ] Error handling works as expected
- [ ] Rate limiting is functional
- [ ] CORS headers are present
- [ ] Health check endpoint is accessible

## 🚀 Next Steps

1. **Run the tests** to verify everything works
2. **Import the Postman collection** for manual testing
3. **Review the OpenAPI documentation** for API details
4. **Use the test scripts** for automated validation
5. **Update documentation** as the API evolves

## 📞 Support

If you encounter issues:
1. Check the troubleshooting section above
2. Review the existing documentation in the `docs/` folder
3. Run the test scripts to identify specific problems
4. Check the service logs for detailed error information 