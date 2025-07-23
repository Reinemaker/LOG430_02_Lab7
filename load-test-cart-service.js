import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 10 }, // Ramp up to 10 users
    { duration: '1m', target: 10 },  // Stay at 10 users
    { duration: '30s', target: 0 },  // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests must complete below 500ms
    http_req_failed: ['rate<0.1'],    // Error rate must be less than 10%
  },
};

const BASE_URL = 'http://cart.cornershop.localhost';
const API_KEY = 'cornershop-api-key-2024';

export default function () {
  const headers = {
    'X-API-Key': API_KEY,
    'Content-Type': 'application/json',
  };

  // Test cart health endpoint
  const healthResponse = http.get(`${BASE_URL}/health`, { headers });
  check(healthResponse, {
    'health check status is 200': (r) => r.status === 200,
    'health check response time < 200ms': (r) => r.timings.duration < 200,
  });

  // Test cart endpoints
  const cartResponse = http.get(`${BASE_URL}/api/cart`, { headers });
  check(cartResponse, {
    'cart endpoint status is 200': (r) => r.status === 200,
    'cart endpoint response time < 500ms': (r) => r.timings.duration < 500,
  });

  // Add some random cart operations
  const cartId = Math.floor(Math.random() * 1000);
  const addItemResponse = http.post(`${BASE_URL}/api/cart/${cartId}/items`, 
    JSON.stringify({
      productId: Math.floor(Math.random() * 100),
      quantity: Math.floor(Math.random() * 5) + 1
    }), 
    { headers }
  );
  
  check(addItemResponse, {
    'add item status is 200 or 201': (r) => r.status === 200 || r.status === 201,
  });

  sleep(1);
} 