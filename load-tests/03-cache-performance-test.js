import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');
const requestsPerSecond = new Counter('requests_per_second');
const cacheHitRate = new Rate('cache_hits');
const cacheMissRate = new Rate('cache_misses');

// Test configuration for cache performance testing
export const options = {
  stages: [
    { duration: '1m', target: 10 },   // Warm up
    { duration: '2m', target: 10 },   // Baseline without cache
    { duration: '1m', target: 20 },   // Ramp up
    { duration: '2m', target: 20 },   // Test with cache
    { duration: '1m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.1'],
    errors: ['rate<0.1'],
    cache_hits: ['rate>0.7'], // Expect 70% cache hit rate
  },
};

// Test data
const BASE_URL = __ENV.BASE_URL || 'http://cornershop.localhost';
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

// Test critical endpoints that should be cached

// 1. Store stock endpoint (highly requested)
export function storeStockTest() {
  const storeId = getRandomStoreId();
  
  const response = http.get(`${BASE_URL}/api/v1/products/store/${storeId}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'store stock status is 200': (r) => r.status === 200,
    'store stock response time < 500ms': (r) => r.timings.duration < 500,
    'store stock has cache headers': (r) => {
      return r.headers['Cache-Control'] || r.headers['ETag'] || r.headers['Last-Modified'];
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  // Check if response was cached (faster response time indicates cache hit)
  const isCacheHit = response.timings.duration < 200;
  cacheHitRate.add(isCacheHit);
  cacheMissRate.add(!isCacheHit);
  
  sleep(0.5);
}

// 2. Consolidated sales report (expensive operation)
export function salesReportTest() {
  const startDate = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
  const endDate = new Date().toISOString().split('T')[0];
  
  const response = http.get(`${BASE_URL}/api/v1/reports/sales/consolidated?startDate=${startDate}&endDate=${endDate}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'sales report status is 200': (r) => r.status === 200,
    'sales report response time < 2000ms': (r) => r.timings.duration < 2000,
    'sales report has cache headers': (r) => {
      return r.headers['Cache-Control'] || r.headers['ETag'] || r.headers['Last-Modified'];
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  // Check if response was cached
  const isCacheHit = response.timings.duration < 1000;
  cacheHitRate.add(isCacheHit);
  cacheMissRate.add(!isCacheHit);
  
  sleep(1);
}

// 3. Inventory report (frequently accessed)
export function inventoryReportTest() {
  const response = http.get(`${BASE_URL}/api/v1/reports/inventory`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'inventory report status is 200': (r) => r.status === 200,
    'inventory report response time < 1500ms': (r) => r.timings.duration < 1500,
    'inventory report has cache headers': (r) => {
      return r.headers['Cache-Control'] || r.headers['ETag'] || r.headers['Last-Modified'];
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  // Check if response was cached
  const isCacheHit = response.timings.duration < 800;
  cacheHitRate.add(isCacheHit);
  cacheMissRate.add(!isCacheHit);
  
  sleep(0.8);
}

// 4. Top selling products (expensive aggregation)
export function topSellingProductsTest() {
  const response = http.get(`${BASE_URL}/api/v1/reports/products/top-selling?limit=10`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'top selling products status is 200': (r) => r.status === 200,
    'top selling products response time < 1800ms': (r) => r.timings.duration < 1800,
    'top selling products has cache headers': (r) => {
      return r.headers['Cache-Control'] || r.headers['ETag'] || r.headers['Last-Modified'];
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  // Check if response was cached
  const isCacheHit = response.timings.duration < 1000;
  cacheHitRate.add(isCacheHit);
  cacheMissRate.add(!isCacheHit);
  
  sleep(1);
}

// 5. Store search (frequently used)
export function storeSearchTest() {
  const searchTerms = ['store', 'shop', 'market', 'retail'];
  const searchTerm = searchTerms[Math.floor(Math.random() * searchTerms.length)];
  
  const response = http.get(`${BASE_URL}/api/v1/stores/search?searchTerm=${searchTerm}`, {
    headers: getHeaders(),
  });
  
  check(response, {
    'store search status is 200': (r) => r.status === 200,
    'store search response time < 800ms': (r) => r.timings.duration < 800,
    'store search has cache headers': (r) => {
      return r.headers['Cache-Control'] || r.headers['ETag'] || r.headers['Last-Modified'];
    },
  });
  
  errorRate.add(response.status !== 200);
  responseTime.add(response.timings.duration);
  requestsPerSecond.add(1);
  
  // Check if response was cached
  const isCacheHit = response.timings.duration < 400;
  cacheHitRate.add(isCacheHit);
  cacheMissRate.add(!isCacheHit);
  
  sleep(0.6);
}

// Main test function
export default function() {
  const scenarios = [
    storeStockTest,
    salesReportTest,
    inventoryReportTest,
    topSellingProductsTest,
    storeSearchTest,
  ];
  
  // Randomly select a scenario based on weights
  const weights = [0.4, 0.2, 0.2, 0.1, 0.1]; // 40% store stock, 20% reports, etc.
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

// Setup function
export function setup() {
  console.log('Setting up cache performance test...');
  console.log(`Base URL: ${BASE_URL}`);
  
  // Test basic connectivity
  const healthResponse = http.get(`${BASE_URL}/health`);
  if (healthResponse.status !== 200) {
    throw new Error(`Health check failed: ${healthResponse.status}`);
  }
  
  // Clear cache before testing
  console.log('Clearing cache before testing...');
  
  console.log('Cache performance test setup completed');
  return { baseUrl: BASE_URL };
}

// Teardown function
export function teardown(data) {
  console.log('Cache performance test completed');
  console.log(`Final base URL: ${data.baseUrl}`);
} 