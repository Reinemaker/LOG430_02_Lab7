using CornerShop.Models;

namespace CornerShop.Services
{
    /// <summary>
    /// Implementation of controlled failure service for testing saga orchestration
    /// </summary>
    public class ControlledFailureService : IControlledFailureService
    {
        private readonly ISagaEventPublisher _eventPublisher;
        private readonly ISagaMetricsService _metricsService;
        private readonly IBusinessEventLogger _businessLogger;
        private readonly ILogger<ControlledFailureService> _logger;
        private readonly Dictionary<string, object> _failureConfig;
        private readonly Random _random;

        public ControlledFailureService(ISagaEventPublisher eventPublisher, ISagaMetricsService metricsService, IBusinessEventLogger businessLogger, ILogger<ControlledFailureService> logger)
        {
            _eventPublisher = eventPublisher;
            _metricsService = metricsService;
            _businessLogger = businessLogger;
            _logger = logger;
            _random = new Random();

            // Default failure configuration
            _failureConfig = new Dictionary<string, object>
            {
                ["InsufficientStockProbability"] = 0.1, // 10% chance
                ["PaymentFailureProbability"] = 0.05,   // 5% chance
                ["NetworkTimeoutProbability"] = 0.03,   // 3% chance
                ["DatabaseFailureProbability"] = 0.02,  // 2% chance
                ["ServiceUnavailableProbability"] = 0.01, // 1% chance
                ["EnableFailures"] = true,
                ["FailureDelayMs"] = 1000, // 1 second delay for failures
                ["CriticalProducts"] = new List<string> { "Premium Coffee", "Organic Milk" },
                ["CriticalStores"] = new List<string> { "store_001", "store_002" }
            };
        }

        public async Task<bool> SimulateInsufficientStockAsync(string productName, string storeId, int requestedQuantity, string? sagaId = null)
        {
            if (!_failureConfig.ContainsKey("EnableFailures") || !(bool)_failureConfig["EnableFailures"])
                return false;

            var probability = (double)_failureConfig["InsufficientStockProbability"];
            var criticalProducts = (List<string>)_failureConfig["CriticalProducts"];
            var criticalStores = (List<string>)_failureConfig["CriticalStores"];

            // Increase probability for critical products or stores
            if (criticalProducts.Contains(productName) || criticalStores.Contains(storeId))
            {
                probability *= 2;
            }

            if (_random.NextDouble() < probability)
            {
                var delay = (int)_failureConfig["FailureDelayMs"];
                await Task.Delay(delay);

                var errorMessage = $"Controlled failure: Insufficient stock for {productName} in store {storeId}. Requested: {requestedQuantity}";

                // Record metrics and structured logging
                _metricsService.RecordControlledFailure("insufficient_stock", "ProductService", sagaId ?? "unknown");
                _businessLogger.LogControlledFailure(sagaId ?? "unknown", "insufficient_stock", "ProductService", new Dictionary<string, object>
                {
                    ["product_name"] = productName,
                    ["store_id"] = storeId,
                    ["requested_quantity"] = requestedQuantity,
                    ["probability"] = probability
                });

                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ControlledFailureService", "SimulateInsufficientStock", SagaEventType.Failure, errorMessage, new { ProductName = productName, StoreId = storeId, RequestedQuantity = requestedQuantity });
                }

                _logger.LogWarning("Controlled failure triggered: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return false;
        }

        public async Task<bool> SimulatePaymentFailureAsync(decimal amount, string customerId, string? sagaId = null)
        {
            if (!_failureConfig.ContainsKey("EnableFailures") || !(bool)_failureConfig["EnableFailures"])
                return false;

            var probability = (double)_failureConfig["PaymentFailureProbability"];

            // Increase probability for high amounts
            if (amount > 1000)
            {
                probability *= 1.5;
            }

            if (_random.NextDouble() < probability)
            {
                var delay = (int)_failureConfig["FailureDelayMs"];
                await Task.Delay(delay);

                var errorMessage = $"Controlled failure: Payment failed for customer {customerId}. Amount: {amount:C}";

                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ControlledFailureService", "SimulatePaymentFailure", SagaEventType.Failure, errorMessage, new { CustomerId = customerId, Amount = amount });
                }

                _logger.LogWarning("Controlled failure triggered: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return false;
        }

        public async Task<bool> SimulateNetworkTimeoutAsync(string serviceName, string? sagaId = null)
        {
            if (!_failureConfig.ContainsKey("EnableFailures") || !(bool)_failureConfig["EnableFailures"])
                return false;

            var probability = (double)_failureConfig["NetworkTimeoutProbability"];

            if (_random.NextDouble() < probability)
            {
                var delay = (int)_failureConfig["FailureDelayMs"];
                await Task.Delay(delay);

                var errorMessage = $"Controlled failure: Network timeout for service {serviceName}";

                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ControlledFailureService", "SimulateNetworkTimeout", SagaEventType.Failure, errorMessage, new { ServiceName = serviceName });
                }

                _logger.LogWarning("Controlled failure triggered: {ErrorMessage}", errorMessage);
                throw new TimeoutException(errorMessage);
            }

            return false;
        }

        public async Task<bool> SimulateDatabaseFailureAsync(string operation, string? sagaId = null)
        {
            if (!_failureConfig.ContainsKey("EnableFailures") || !(bool)_failureConfig["EnableFailures"])
                return false;

            var probability = (double)_failureConfig["DatabaseFailureProbability"];

            if (_random.NextDouble() < probability)
            {
                var delay = (int)_failureConfig["FailureDelayMs"];
                await Task.Delay(delay);

                var errorMessage = $"Controlled failure: Database connection failed for operation {operation}";

                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ControlledFailureService", "SimulateDatabaseFailure", SagaEventType.Failure, errorMessage, new { Operation = operation });
                }

                _logger.LogWarning("Controlled failure triggered: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return false;
        }

        public async Task<bool> SimulateServiceUnavailableAsync(string serviceName, string? sagaId = null)
        {
            if (!_failureConfig.ContainsKey("EnableFailures") || !(bool)_failureConfig["EnableFailures"])
                return false;

            var probability = (double)_failureConfig["ServiceUnavailableProbability"];

            if (_random.NextDouble() < probability)
            {
                var delay = (int)_failureConfig["FailureDelayMs"];
                await Task.Delay(delay);

                var errorMessage = $"Controlled failure: Service {serviceName} is unavailable";

                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ControlledFailureService", "SimulateServiceUnavailable", SagaEventType.Failure, errorMessage, new { ServiceName = serviceName });
                }

                _logger.LogWarning("Controlled failure triggered: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return false;
        }

        public Dictionary<string, object> GetFailureConfiguration()
        {
            return new Dictionary<string, object>(_failureConfig);
        }

        public void UpdateFailureConfiguration(Dictionary<string, object> config)
        {
            foreach (var kvp in config)
            {
                _failureConfig[kvp.Key] = kvp.Value;
            }

            _logger.LogInformation("Failure configuration updated: {Config}", string.Join(", ", config.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }
    }
}
