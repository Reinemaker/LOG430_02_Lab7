# Load Balancing Implementation for CornerShop Microservices

## Overview

This document describes the implementation of load balancing for the CornerShop microservices architecture using Traefik as the API Gateway and load balancer.

## Architecture

### Load Balancing Strategy
- **Algorithm**: Round-robin (default Traefik behavior)
- **Service**: Cart Service with 3 instances
- **Load Balancer**: Traefik v2.10
- **Health Checks**: Automatic health monitoring

### Service Instances
```
cart-service-1 (Instance 1)
cart-service-2 (Instance 2)  
cart-service-3 (Instance 3)
```

## Implementation Details

### 1. Docker Compose Configuration

The load balancing is configured in `docker-compose.microservices.yml`:

```yaml
# Shopping Cart Service - Instance 1
cart-service-1:
  build:
    context: .
    dockerfile: services/CartService/Dockerfile
  container_name: cart-service-1
  environment:
    - SERVICE_INSTANCE=1
  labels:
    - "traefik.enable=true"
    - "traefik.http.routers.cart-service.rule=Host(`cart.cornershop.localhost`)"
    - "traefik.http.services.cart-service.loadbalancer.server.scheme=http"
    - "traefik.http.services.cart-service.loadbalancer.server.port=80"

# Shopping Cart Service - Instance 2
cart-service-2:
  # Similar configuration with SERVICE_INSTANCE=2

# Shopping Cart Service - Instance 3  
cart-service-3:
  # Similar configuration with SERVICE_INSTANCE=3
```

### 2. Traefik Configuration

Traefik automatically detects multiple containers with the same service name and applies round-robin load balancing:

- **Service Name**: `cart-service`
- **Router Rule**: `Host(cart.cornershop.localhost)`
- **Load Balancer**: Automatic round-robin distribution
- **Health Checks**: Built-in container health monitoring

### 3. Health Monitoring

Each cart service instance includes a health endpoint that returns instance-specific information:

```json
{
  "status": "healthy",
  "service": "cart-service", 
  "instance": "1",
  "hostname": "cart-service-1",
  "timestamp": "2024-01-01T12:00:00Z",
  "version": "1.0.0",
  "uptime": 123456
}
```

## Testing Load Balancing

### 1. Manual Testing with curl

```bash
# Test individual instances
curl -H "X-API-Key: cornershop-api-key-2024" http://cart.cornershop.localhost/health

# Run load balancing test script
./test-load-balancing.sh
```

### 2. Automated Testing with k6

```bash
# Install k6 (if not already installed)
# Run load test
k6 run load-test-cart-service.js
```

### 3. Expected Load Distribution

With round-robin load balancing, requests should be distributed approximately evenly:

- Instance 1: ~33.3% of requests
- Instance 2: ~33.3% of requests  
- Instance 3: ~33.3% of requests

## Monitoring and Observability

### 1. Traefik Dashboard

Access Traefik dashboard to monitor load balancing:
- URL: http://traefik.localhost:8080
- Shows active services, routers, and load balancer status

### 2. Grafana Dashboards

Monitor load balancing metrics in Grafana:
- URL: http://localhost:3000
- Default credentials: admin/admin

### 3. Prometheus Metrics

Prometheus collects metrics from Traefik and services:
- URL: http://localhost:9090
- Metrics include request distribution, response times, error rates

## Performance Characteristics

### Load Balancing Benefits

1. **High Availability**: If one instance fails, others continue serving requests
2. **Scalability**: Easy to add/remove instances without downtime
3. **Performance**: Distributes load across multiple instances
4. **Fault Tolerance**: Automatic failover to healthy instances

### Expected Performance Metrics

- **Response Time**: < 500ms (95th percentile)
- **Error Rate**: < 1%
- **Throughput**: Improved by ~3x with 3 instances
- **Availability**: 99.9%+ with proper health checks

## Troubleshooting

### Common Issues

1. **Uneven Load Distribution**
   - Check Traefik logs: `docker logs traefik`
   - Verify all instances are healthy
   - Check service labels configuration

2. **Instance Not Receiving Requests**
   - Verify container is running: `docker ps`
   - Check health endpoint: `curl http://instance:80/health`
   - Review Traefik service discovery

3. **High Response Times**
   - Monitor resource usage: `docker stats`
   - Check database connections
   - Review application logs

### Debug Commands

```bash
# Check service status
docker compose -f docker-compose.microservices.yml ps

# View Traefik logs
docker logs traefik

# Check individual instance logs
docker logs cart-service-1
docker logs cart-service-2
docker logs cart-service-3

# Test load balancing manually
for i in {1..10}; do
  curl -s -H "X-API-Key: cornershop-api-key-2024" \
    http://cart.cornershop.localhost/health | jq '.instance'
done
```

## Scaling Considerations

### Horizontal Scaling

To add more instances:

1. Add new service definition in docker-compose.yml
2. Update SERVICE_INSTANCE environment variable
3. Restart services: `docker compose up -d`

### Vertical Scaling

To increase instance capacity:

1. Adjust container resource limits
2. Optimize application performance
3. Monitor resource utilization

## Security Considerations

1. **API Key Authentication**: All requests require valid API key
2. **Rate Limiting**: Traefik middleware prevents abuse
3. **Network Isolation**: Services communicate via internal Docker network
4. **Health Check Security**: Health endpoints don't expose sensitive data

## Future Enhancements

1. **Advanced Load Balancing**: Implement weighted round-robin or least connections
2. **Circuit Breaker**: Add circuit breaker pattern for fault tolerance
3. **Auto-scaling**: Implement automatic scaling based on metrics
4. **Blue-Green Deployment**: Support zero-downtime deployments
5. **Canary Deployments**: Gradual rollout of new versions

## Conclusion

The load balancing implementation provides:
- ✅ Round-robin distribution across 3 cart service instances
- ✅ Automatic health monitoring and failover
- ✅ Comprehensive testing and monitoring tools
- ✅ Scalable architecture for future growth
- ✅ High availability and fault tolerance

This implementation meets all requirements for the CornerShop microservices architecture and provides a solid foundation for production deployment. 