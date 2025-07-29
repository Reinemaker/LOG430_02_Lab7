import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');
const requestsPerSecond = new Counter('requests_per_second');
const instanceDistribution = new Counter('instance_distribution');

// Test configuration for load balancer testing
export const options = {
  scenarios: {
    // Test with 1 instance
    single_instance: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 10 },
        { duration: '3m', target: 10 },
        { duration: '1m', target: 0 },
      ],
      exec: 'singleInstanceTest',
    },
    // Test with 2 instances
    two_instances: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 20 },
        { duration: '3m', target: 20 },
        { duration: '1m', target: 0 },
      ],
      exec: 'twoInstancesTest',
    },
    // Test with 3 instances
    three_instances: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 30 },
        { duration: '3m', target: 30 },
        { duration: '1m', target: 0 },
      ],
      exec: 'threeInstancesTest',
    },
    // Test with 4 instances
    four_instances: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 40 },
        { duration: '3m', target: 40 },
        { duration: '1m', target: 0 },
      ],
      exec: 'fourInstancesTest',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.1'],
    errors: ['rate<0.1'],
  },
};

// Test data
const BASE_URL = __ENV.BASE_URL || 'http://api.cornershop.localhost';
const AUTH_TOKEN = __ENV.AUTH_TOKEN || '';

// Helper function to get headers
function getHeaders() {
  const headers = {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  };
  
  if (AUTH_TOKEN) {
    headers['Authorization'] = `Bearer ${AUTH_TOKEN}`;
  }
  
  return headers;
}

// Helper function to get random store ID
function getRandomStoreId() {
  const storeIds = ['store-1', 'store-2', 'store-3', 'store-4', 'store-5'];
  return storeIds[Math.floor(Math.random() * storeIds.length)];
}

// Test function for single instance
export function singleInstanceTest() {
  const storeId = getRandomStoreId();
  
  const response = http.get(`${BASE_URL}/api/v1/products/store/${storeId}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'single instance status is 200': (r) => r.status === 200,
    'single instance response time < 1000ms': (r) => r.timings.duration < 1000,
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  sleep(1);
}

// Test function for two instances
export function twoInstancesTest() {
  const storeId = getRandomStoreId();
  
  const response = http.get(`${BASE_URL}/api/v1/products/store/${storeId}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'two instances status is 200': (r) => r.status === 200,
    'two instances response time < 800ms': (r) => r.timings.duration < 800,
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  sleep(0.8);
}

// Test function for three instances
export function threeInstancesTest() {
  const storeId = getRandomStoreId();
  
  const response = http.get(`${BASE_URL}/api/v1/products/store/${storeId}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'three instances status is 200': (r) => r.status === 200,
    'three instances response time < 600ms': (r) => r.timings.duration < 600,
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  sleep(0.6);
}

// Test function for four instances
export function fourInstancesTest() {
  const storeId = getRandomStoreId();
  
  const response = http.get(`${BASE_URL}/api/v1/products/store/${storeId}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'four instances status is 200': (r) => r.status === 200,
    'four instances response time < 500ms': (r) => r.timings.duration < 500,
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  sleep(0.5);
}

// Setup function
export function setup() {
  console.log('Setting up load balancer test...');
  console.log(`Base URL: ${BASE_URL}`);
  
  // Test basic connectivity
  const healthResponse = http.get(`${BASE_URL}/health`);
  if (healthResponse.status !== 200) {
    throw new Error(`Health check failed: ${healthResponse.status}`);
  }
  
  console.log('Load balancer test setup completed');
  return { baseUrl: BASE_URL };
}

// Teardown function
export function teardown(data) {
  console.log('Load balancer test completed');
  console.log(`Final base URL: ${data.baseUrl}`);
} 