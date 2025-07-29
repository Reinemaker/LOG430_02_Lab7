import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics for comparison
const directApiLatency = new Trend('direct_api_latency');
const gatewayApiLatency = new Trend('gateway_api_latency');
const directApiErrors = new Rate('direct_api_errors');
const gatewayApiErrors = new Rate('gateway_api_errors');
const directApiSuccess = new Counter('direct_api_success');
const gatewayApiSuccess = new Counter('gateway_api_success');

export const options = {
  stages: [
    { duration: '1m', target: 10 },   // Warm up
    { duration: '2m', target: 50 },   // Normal load
    { duration: '2m', target: 100 },  // Medium load
    { duration: '2m', target: 200 },  // High load
    { duration: '1m', target: 0 },    // Cool down
  ],
  thresholds: {
    'direct_api_latency': ['p(95)<2000'],     // 95% of direct API calls < 2s
    'gateway_api_latency': ['p(95)<3000'],    // 95% of gateway API calls < 3s
    'direct_api_errors': ['rate<0.05'],       // Direct API error rate < 5%
    'gateway_api_errors': ['rate<0.05'],      // Gateway API error rate < 5%
    'http_req_duration': ['p(95)<5000'],      // Overall 95% < 5s
    'http_req_failed': ['rate<0.1'],          // Overall error rate < 10%
  },
};

// Configuration
const GATEWAY_BASE_URL = __ENV.GATEWAY_BASE_URL || 'http://api.cornershop.localhost';
const API_KEY = __ENV.API_KEY || 'cornershop-api-key-2024';

// Test data
const testProducts = [
  { id: '1', name: 'Test Product 1' },
  { id: '2', name: 'Test Product 2' },
  { id: '3', name: 'Test Product 3' },
];

const testCustomers = [
  { id: '1', name: 'John Doe' },
  { id: '2', name: 'Jane Smith' },
];

export default function() {
  sleep(0.5);
  
  // Test API Gateway calls
  group('API Gateway Calls', function() {
    // Test products endpoint via gateway
    const gatewayProductsResponse = http.get(`${GATEWAY_BASE_URL}/api/products`, {
      headers: {
        'X-API-Key': API_KEY,
      },
    });
    gatewayApiLatency.add(gatewayProductsResponse.timings.duration);
    
    check(gatewayProductsResponse, {
      'gateway products status is 200': (r) => r.status === 200,
      'gateway products response time < 3s': (r) => r.timings.duration < 3000,
      'gateway products has gateway headers': (r) => r.headers['X-Gateway-Version'] !== undefined,
    });
    
    if (gatewayProductsResponse.status === 200) {
      gatewayApiSuccess.add(1);
    } else {
      gatewayApiErrors.add(1);
    }
    
    // Test customers endpoint via gateway
    const gatewayCustomersResponse = http.get(`${GATEWAY_BASE_URL}/api/customers`, {
      headers: {
        'X-API-Key': API_KEY,
      },
    });
    gatewayApiLatency.add(gatewayCustomersResponse.timings.duration);
    
    check(gatewayCustomersResponse, {
      'gateway customers status is 200': (r) => r.status === 200,
      'gateway customers response time < 3s': (r) => r.timings.duration < 3000,
      'gateway customers has gateway headers': (r) => r.headers['X-Gateway-Version'] !== undefined,
    });
    
    if (gatewayCustomersResponse.status === 200) {
      gatewayApiSuccess.add(1);
    } else {
      gatewayApiErrors.add(1);
    }
    
    // Test cart endpoint via gateway
    const gatewayCartResponse = http.get(`${GATEWAY_BASE_URL}/api/cart?customerId=1`, {
      headers: {
        'X-API-Key': API_KEY,
      },
    });
    gatewayApiLatency.add(gatewayCartResponse.timings.duration);
    
    check(gatewayCartResponse, {
      'gateway cart status is 200': (r) => r.status === 200,
      'gateway cart response time < 3s': (r) => r.timings.duration < 3000,
      'gateway cart has gateway headers': (r) => r.headers['X-Gateway-Version'] !== undefined,
    });
    
    if (gatewayCartResponse.status === 200) {
      gatewayApiSuccess.add(1);
    } else {
      gatewayApiErrors.add(1);
    }
  });
  
  sleep(1);
}

// Handle setup and teardown
export function setup() {
  console.log('Starting Architecture Comparison Test');
  console.log(`Direct API URL: ${DIRECT_BASE_URL}`);
  console.log(`Gateway API URL: ${GATEWAY_BASE_URL}`);
  console.log('Testing both architectures for performance comparison...');
}

export function teardown(data) {
  console.log('Architecture Comparison Test completed');
  console.log('Check the metrics for detailed comparison results');
} 