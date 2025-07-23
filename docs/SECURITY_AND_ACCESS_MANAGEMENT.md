# Security and Access Management for CornerShop API Gateway

## Overview

This document describes the comprehensive security and access management implementation for the CornerShop microservices architecture using Nginx as the API Gateway.

## Security Features Implemented

### 1. CORS (Cross-Origin Resource Sharing) Configuration

#### Allowed Origins
```nginx
add_header Access-Control-Allow-Origin "https://cornershop.localhost, http://localhost:3000, http://localhost:8080" always;
```

#### Allowed Methods
```nginx
add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
```

#### Allowed Headers
```nginx
add_header Access-Control-Allow-Headers "Origin, X-Requested-With, Content-Type, Accept, Authorization, X-API-Key" always;
```

#### Preflight Request Handling
- Automatic handling of OPTIONS requests
- 204 response for preflight requests
- 24-hour cache for preflight results

### 2. Security Headers

#### X-Frame-Options
```nginx
add_header X-Frame-Options "SAMEORIGIN" always;
```
- Prevents clickjacking attacks
- Only allows framing from same origin

#### X-Content-Type-Options
```nginx
add_header X-Content-Type-Options "nosniff" always;
```
- Prevents MIME type sniffing
- Forces browsers to respect declared content type

#### X-XSS-Protection
```nginx
add_header X-XSS-Protection "1; mode=block" always;
```
- Enables XSS filtering
- Blocks rendering if XSS attack detected

#### Referrer-Policy
```nginx
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
```
- Controls referrer information in requests
- Balances security and functionality

#### Content-Security-Policy
```nginx
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';" always;
```
- Restricts resource loading
- Prevents XSS and injection attacks

### 3. API Key Authentication

#### Validation Logic
```nginx
# Check for API key in header
if ($http_x_api_key = "") {
    return 401 '{"error": "API key required", "code": "MISSING_API_KEY"}';
}

# Validate API key
if ($http_x_api_key != "cornershop-api-key-2024") {
    return 403 '{"error": "Invalid API key", "code": "INVALID_API_KEY"}';
}
```

#### Error Responses
- **401 Unauthorized**: Missing API key
- **403 Forbidden**: Invalid API key
- Structured JSON responses with error codes

### 4. Rate Limiting and Throttling

#### Rate Limiting Zones
```nginx
# General API rate limiting
limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;

# Strict rate limiting for sensitive operations
limit_req_zone $binary_remote_addr zone=strict:10m rate=5r/s;

# API key-based rate limiting
limit_req_zone $http_x_api_key zone=api_key:10m rate=20r/s;
```

#### Service-Specific Limits
- **Product Service**: 5 requests/second (strict)
- **Customer Service**: 5 requests/second (strict)
- **Cart Service**: 10 requests/second (standard)
- **Order Service**: 5 requests/second (strict)

#### Burst Handling
```nginx
limit_req zone=api burst=20 nodelay;
limit_req zone=strict burst=10 nodelay;
limit_req zone=api_key burst=30 nodelay;
```

### 5. Connection Limiting

#### Connection Limits
```nginx
limit_conn_zone $binary_remote_addr zone=conn_limit:10m;
limit_conn conn_limit 10;
```
- Maximum 10 concurrent connections per IP
- Prevents connection exhaustion attacks

### 6. Access Logging and Monitoring

#### Enhanced Logging Formats
```nginx
# Security-focused logging
log_format security '$remote_addr - $remote_user [$time_local] "$request" '
                   '$status $body_bytes_sent "$http_referer" '
                   '"$http_user_agent" "$http_x_forwarded_for" '
                   'upstream: $upstream_addr '
                   'api_key: $http_x_api_key '
                   'request_id: $request_id '
                   'response_time: $request_time';

# Standard logging
log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                '$status $body_bytes_sent "$http_referer" '
                '"$http_user_agent" "$http_x_forwarded_for" '
                'upstream: $upstream_addr';
```

#### Log Files
- `/var/log/nginx/access.log` - Standard access logs
- `/var/log/nginx/security.log` - Security-focused logs
- `/var/log/nginx/error.log` - Error logs

### 7. Quota Management

#### Request Quotas
- **Per IP**: 10 requests/second general, 5 requests/second strict
- **Per API Key**: 20 requests/second
- **Per Service**: Varies by sensitivity level

#### Connection Quotas
- **Per IP**: 10 concurrent connections
- **Timeout**: 30 seconds for proxy operations

## Service-Specific Security

### Product Service
```nginx
location /api/products {
    # Strict rate limiting for product service
    limit_req zone=strict burst=10 nodelay;
    
    # Enhanced headers
    proxy_set_header X-API-Key $http_x_api_key;
    proxy_set_header X-Request-ID $request_id;
    
    # Timeout settings
    proxy_connect_timeout 30s;
    proxy_send_timeout 30s;
    proxy_read_timeout 30s;
}
```

### Customer Service
```nginx
location /api/customers {
    # Customer data requires strict security
    limit_req zone=strict burst=5 nodelay;
    
    # Enhanced headers
    proxy_set_header X-API-Key $http_x_api_key;
    proxy_set_header X-Request-ID $request_id;
    
    # Timeout settings
    proxy_connect_timeout 30s;
    proxy_send_timeout 30s;
    proxy_read_timeout 30s;
}
```

### Cart Service
```nginx
location /api/cart {
    # Cart service with standard rate limiting
    limit_req zone=api burst=20 nodelay;
    
    # Enhanced headers
    proxy_set_header X-API-Key $http_x_api_key;
    proxy_set_header X-Request-ID $request_id;
    
    # Timeout settings
    proxy_connect_timeout 30s;
    proxy_send_timeout 30s;
    proxy_read_timeout 30s;
}
```

### Order Service
```nginx
location /api/orders {
    # Order service with strict rate limiting
    limit_req zone=strict burst=10 nodelay;
    
    # Enhanced headers
    proxy_set_header X-API-Key $http_x_api_key;
    proxy_set_header X-Request-ID $request_id;
    
    # Timeout settings
    proxy_connect_timeout 30s;
    proxy_send_timeout 30s;
    proxy_read_timeout 30s;
}
```

## Testing Security Features

### 1. CORS Testing
```bash
# Test preflight request
curl -X OPTIONS -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: X-API-Key" \
  http://api.cornershop.localhost/api/products

# Test actual request
curl -H "Origin: http://localhost:3000" \
  -H "X-API-Key: cornershop-api-key-2024" \
  http://api.cornershop.localhost/api/products
```

### 2. API Key Testing
```bash
# Test missing API key
curl http://api.cornershop.localhost/api/products

# Test invalid API key
curl -H "X-API-Key: invalid-key" \
  http://api.cornershop.localhost/api/products

# Test valid API key
curl -H "X-API-Key: cornershop-api-key-2024" \
  http://api.cornershop.localhost/api/products
```

### 3. Rate Limiting Testing
```bash
# Test rate limiting
for i in {1..15}; do
  curl -H "X-API-Key: cornershop-api-key-2024" \
    http://api.cornershop.localhost/api/customers
  echo "Request $i"
  sleep 0.1
done
```

### 4. Security Headers Testing
```bash
# Check security headers
curl -I -H "X-API-Key: cornershop-api-key-2024" \
  http://api.cornershop.localhost/api/products
```

## Monitoring and Alerting

### 1. Security Log Analysis
```bash
# Monitor security logs
tail -f /var/log/nginx/security.log

# Analyze failed authentication attempts
grep "INVALID_API_KEY" /var/log/nginx/security.log

# Monitor rate limiting violations
grep "429" /var/log/nginx/access.log
```

### 2. Grafana Dashboards
- **Security Metrics**: Failed auth attempts, rate limiting violations
- **Performance Metrics**: Response times, throughput
- **Error Rates**: 401, 403, 429 status codes

### 3. Prometheus Metrics
- Request rates by endpoint
- Error rates by type
- Response time percentiles
- Rate limiting violations

## Security Best Practices

### 1. API Key Management
- Rotate API keys regularly
- Use different keys for different environments
- Implement key expiration
- Monitor key usage patterns

### 2. Rate Limiting Tuning
- Adjust limits based on usage patterns
- Monitor for legitimate traffic being blocked
- Implement whitelisting for trusted clients
- Use different limits for different user tiers

### 3. CORS Configuration
- Restrict allowed origins to minimum required
- Avoid using wildcards in production
- Regularly review and update allowed origins
- Monitor for CORS-related errors

### 4. Logging and Monitoring
- Regularly review security logs
- Set up alerts for suspicious activity
- Monitor for unusual traffic patterns
- Implement log retention policies

## Troubleshooting

### Common Issues

1. **CORS Errors**
   - Check allowed origins configuration
   - Verify preflight request handling
   - Review browser console for specific errors

2. **Rate Limiting Too Strict**
   - Adjust rate limiting zones
   - Increase burst limits
   - Implement whitelisting for trusted IPs

3. **API Key Issues**
   - Verify key format and value
   - Check header name (X-API-Key)
   - Review authentication logic

4. **Security Headers Missing**
   - Verify Nginx configuration
   - Check for syntax errors
   - Restart Nginx service

### Debug Commands

```bash
# Check Nginx configuration
nginx -t

# View real-time logs
tail -f /var/log/nginx/security.log

# Test specific endpoints
curl -v -H "X-API-Key: cornershop-api-key-2024" \
  http://api.cornershop.localhost/api/products

# Monitor rate limiting
watch -n 1 'grep "429" /var/log/nginx/access.log | wc -l'
```

## Future Enhancements

1. **JWT Authentication**: Implement JWT-based authentication
2. **OAuth2 Integration**: Add OAuth2 support for third-party applications
3. **IP Whitelisting**: Implement IP-based access control
4. **Advanced Rate Limiting**: Implement sliding window rate limiting
5. **Request Signing**: Add request signature validation
6. **API Versioning**: Implement API versioning with security controls
7. **Audit Logging**: Enhanced audit trail for compliance
8. **Real-time Monitoring**: Implement real-time security monitoring

## Conclusion

The security and access management implementation provides:

- ✅ Comprehensive CORS configuration
- ✅ API key authentication
- ✅ Multi-level rate limiting and throttling
- ✅ Connection limiting
- ✅ Enhanced security headers
- ✅ Detailed access logging
- ✅ Service-specific security policies
- ✅ Monitoring and alerting capabilities

This implementation ensures secure access to the CornerShop microservices while maintaining performance and usability. 