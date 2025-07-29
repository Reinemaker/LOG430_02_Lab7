import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');
const requestsPerSecond = new Counter('requests_per_second');

// Test configuration
export const options = {
  stages: [
    { duration: '2m', target: 10 },   // Ramp up to 10 users
    { duration: '5m', target: 10 },   // Stay at 10 users
    { duration: '2m', target: 20 },   // Ramp up to 20 users
    { duration: '5m', target: 20 },   // Stay at 20 users
    { duration: '2m', target: 50 },   // Ramp up to 50 users
    { duration: '5m', target: 50 },   // Stay at 50 users
    { duration: '2m', target: 100 },  // Ramp up to 100 users
    { duration: '5m', target: 100 },  // Stay at 100 users
    { duration: '2m', target: 0 },    // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% of requests must complete below 2s
    http_req_failed: ['rate<0.1'],     // Error rate must be less than 10%
    errors: ['rate<0.1'],              // Custom error rate
  },
};

// Test data
const BASE_URL = __ENV.BASE_URL || 'http://api.cornershop.localhost';
const AUTH_TOKEN = __ENV.AUTH_TOKEN || '';

// Helper function to get random store ID
function getRandomStoreId() {
  const storeIds = ['store-1', 'store-2', 'store-3', 'store-4', 'store-5'];
  return storeIds[Math.floor(Math.random() * storeIds.length)];
}

// Helper function to get random product ID
function getRandomProductId() {
  const productIds = ['product-1', 'product-2', 'product-3', 'product-4', 'product-5'];
  return productIds[Math.floor(Math.random() * productIds.length)];
}

// Helper function to add authentication headers
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

// Scenario 1: Concurrent stock consultation for multiple stores
export function stockConsultationScenario() {
  const storeId = getRandomStoreId();
  
  const response = http.get(`${BASE_URL}/api/v1/products/store/${storeId}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'stock consultation status is 200': (r) => r.status === 200,
    'stock consultation response time < 1000ms': (r) => r.timings.duration < 1000,
    'stock consultation has products': (r) => {
      try {
        const data = JSON.parse(r.body);
        return data.data && Array.isArray(data.data);
      } catch (e) {
        return false;
      }
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  sleep(1);
}

// Scenario 2: Consolidated reports generation
export function consolidatedReportsScenario() {
  const startDate = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
  const endDate = new Date().toISOString().split('T')[0];
  
  const response = http.get(`${BASE_URL}/api/v1/reports/sales/consolidated?startDate=${startDate}&endDate=${endDate}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'consolidated report status is 200': (r) => r.status === 200,
    'consolidated report response time < 3000ms': (r) => r.timings.duration < 3000,
    'consolidated report has data': (r) => {
      try {
        const data = JSON.parse(r.body);
        return data.data && data.data.totalSales !== undefined;
      } catch (e) {
        return false;
      }
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  sleep(2);
}

// Scenario 3: High-frequency product updates
export function productUpdateScenario() {
  const productId = getRandomProductId();
  const storeId = getRandomStoreId();
  
  const updateData = {
    name: `Updated Product ${Date.now()}`,
    price: Math.random() * 1000,
    stockQuantity: Math.floor(Math.random() * 100),
    storeId: storeId,
  };
  
  const response = http.patch(`${BASE_URL}/api/v1/products/${productId}`, JSON.stringify(updateData), {
    headers: getHeaders(),
  });
  
  check(response, {
    'product update status is 200': (r) => r.status === 200,
    'product update response time < 1500ms': (r) => r.timings.duration < 1500,
    'product update successful': (r) => {
      try {
        const data = JSON.parse(r.body);
        return data.data && data.data.id === productId;
      } catch (e) {
        return false;
      }
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  sleep(0.5);
}

// Scenario 4: Inventory report generation
export function inventoryReportScenario() {
  const response = http.get(`${BASE_URL}/api/v1/reports/inventory`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'inventory report status is 200': (r) => r.status === 200,
    'inventory report response time < 2000ms': (r) => r.timings.duration < 2000,
    'inventory report has data': (r) => {
      try {
        const data = JSON.parse(r.body);
        return data.data && data.data.totalProducts !== undefined;
      } catch (e) {
        return false;
      }
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  sleep(1.5);
}

// Scenario 5: Store search and retrieval
export function storeSearchScenario() {
  const searchTerm = ['store', 'shop', 'market', 'retail'][Math.floor(Math.random() * 4)];
  
  const response = http.get(`${BASE_URL}/api/v1/stores/search?searchTerm=${searchTerm}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'store search status is 200': (r) => r.status === 200,
    'store search response time < 1000ms': (r) => r.timings.duration < 1000,
    'store search has results': (r) => {
      try {
        const data = JSON.parse(r.body);
        return data.data && Array.isArray(data.data);
      } catch (e) {
        return false;
      }
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  sleep(0.8);
}

// Main test function
export default function() {
  const scenarios = [
    stockConsultationScenario,
    consolidatedReportsScenario,
    productUpdateScenario,
    inventoryReportScenario,
    storeSearchScenario,
  ];
  
  // Randomly select a scenario based on weights
  const weights = [0.3, 0.2, 0.2, 0.15, 0.15]; // 30% stock consultation, 20% reports, etc.
  const random = Math.random();
  let cumulativeWeight = 0;
  
  for (let i = 0; i < scenarios.length; i++) {
    cumulativeWeight += weights[i];
    if (random <= cumulativeWeight) {
      scenarios[i]();
      break;
    }
  }
}

// Setup function to initialize test data
export function setup() {
  console.log('Setting up load test...');
  console.log(`Base URL: ${BASE_URL}`);
  console.log(`Auth Token: ${AUTH_TOKEN ? 'Provided' : 'Not provided'}`);
  
  // Test basic connectivity
  const healthResponse = http.get(`${BASE_URL}/health`);
  if (healthResponse.status !== 200) {
    throw new Error(`Health check failed: ${healthResponse.status}`);
  }
  
  console.log('Setup completed successfully');
  return { baseUrl: BASE_URL };
}

// Teardown function
export function teardown(data) {
  console.log('Load test completed');
  console.log(`Final base URL: ${data.baseUrl}`);
} 