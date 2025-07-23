using CornerShop.Models;

namespace CornerShop.Services
{
    public class ProductService : IProductService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ISagaEventPublisher _eventPublisher;
        private readonly IControlledFailureService _failureService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IDatabaseService databaseService, ISagaEventPublisher eventPublisher, IControlledFailureService failureService, ILogger<ProductService> logger)
        {
            _databaseService = databaseService;
            _eventPublisher = eventPublisher;
            _failureService = failureService;
            _logger = logger;
        }

        public async Task<List<Product>> SearchProducts(string searchTerm, string? storeId = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));
            return await _databaseService.SearchProducts(searchTerm, storeId);
        }

        public async Task<Product?> GetProductByName(string name, string storeId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            return await _databaseService.GetProductByName(name, storeId);
        }

        public async Task<bool> UpdateStock(string productName, string storeId, int quantity, string? sagaId = null)
        {
            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Product name cannot be empty", nameof(productName));
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            if (quantity == 0)
                throw new ArgumentException("Quantity cannot be zero", nameof(quantity));

            try
            {
                // Simulate controlled failures
                await _failureService.SimulateDatabaseFailureAsync("UpdateStock", sagaId);
                await _failureService.SimulateNetworkTimeoutAsync("ProductService", sagaId);

                var result = await _databaseService.UpdateProductStock(productName, storeId, quantity);

                if (!string.IsNullOrEmpty(sagaId))
                {
                    var eventType = result ? SagaEventType.Success : SagaEventType.Failure;
                    var message = result
                        ? $"Stock updated successfully for {productName} by {quantity}"
                        : $"Failed to update stock for {productName}";

                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ProductService", "UpdateStock", eventType, message, new { ProductName = productName, StoreId = storeId, Quantity = quantity, Result = result });
                }

                return result;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ProductService", "UpdateStock", SagaEventType.Failure, ex.Message, new { ProductName = productName, StoreId = storeId, Quantity = quantity, Error = ex.Message });
                }
                throw;
            }
        }

        public async Task<List<Product>> GetAllProducts(string storeId)
        {
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            return await _databaseService.GetAllProducts(storeId);
        }

        public async Task<bool> ValidateProductExists(string productName, string storeId, string? sagaId = null)
        {
            try
            {
                var product = await GetProductByName(productName, storeId);
                var result = product != null;

                if (!string.IsNullOrEmpty(sagaId))
                {
                    var eventType = result ? SagaEventType.Success : SagaEventType.Failure;
                    var message = result
                        ? $"Product {productName} exists in store {storeId}"
                        : $"Product {productName} not found in store {storeId}";

                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ProductService", "ValidateProductExists", eventType, message, new { ProductName = productName, StoreId = storeId, Exists = result });
                }

                return result;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ProductService", "ValidateProductExists", SagaEventType.Failure, ex.Message, new { ProductName = productName, StoreId = storeId, Error = ex.Message });
                }
                throw;
            }
        }

        public async Task<bool> ValidateStockAvailability(string productName, string storeId, int quantity, string? sagaId = null)
        {
            try
            {
                // Simulate controlled failure for stock validation
                await _failureService.SimulateInsufficientStockAsync(productName, storeId, quantity, sagaId);

                var product = await GetProductByName(productName, storeId);
                if (product == null)
                {
                    if (!string.IsNullOrEmpty(sagaId))
                    {
                        await _eventPublisher.PublishSagaEventAsync(sagaId, "ProductService", "ValidateStockAvailability", SagaEventType.Failure, $"Product {productName} not found", new { ProductName = productName, StoreId = storeId, RequestedQuantity = quantity });
                    }
                    return false;
                }

                var result = product.StockQuantity >= quantity;

                if (!string.IsNullOrEmpty(sagaId))
                {
                    var eventType = result ? SagaEventType.Success : SagaEventType.Failure;
                    var message = result
                        ? $"Stock available for {productName}: {product.StockQuantity} >= {quantity}"
                        : $"Insufficient stock for {productName}: {product.StockQuantity} < {quantity}";

                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ProductService", "ValidateStockAvailability", eventType, message, new { ProductName = productName, StoreId = storeId, AvailableStock = product.StockQuantity, RequestedQuantity = quantity, Sufficient = result });
                }

                return result;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "ProductService", "ValidateStockAvailability", SagaEventType.Failure, ex.Message, new { ProductName = productName, StoreId = storeId, RequestedQuantity = quantity, Error = ex.Message });
                }
                throw;
            }
        }
    }
}
