using CornerShop.Models;

namespace CornerShop.Services
{
    public class SaleService : ISaleService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IProductService _productService;
        private readonly ISagaEventPublisher _eventPublisher;
        private readonly IControlledFailureService _failureService;
        private readonly ILogger<SaleService> _logger;

        public SaleService(IDatabaseService databaseService, IProductService productService, ISagaEventPublisher eventPublisher, IControlledFailureService failureService, ILogger<SaleService> logger)
        {
            _databaseService = databaseService;
            _productService = productService;
            _eventPublisher = eventPublisher;
            _failureService = failureService;
            _logger = logger;
        }

        public async Task<string> CreateSale(Sale sale, string? sagaId = null)
        {
            if (sale == null)
                throw new ArgumentNullException(nameof(sale));

            if (!sale.Items.Any())
                throw new ArgumentException("Sale must have at least one item", nameof(sale));

            try
            {
                if (!await ValidateSaleItems(sale.Items, sale.StoreId, sagaId))
                    throw new InvalidOperationException("One or more items in the sale are invalid");

                // Update stock for each item
                foreach (var item in sale.Items)
                {
                    await _productService.UpdateStock(item.ProductName, sale.StoreId, -item.Quantity, sagaId);
                }

                sale.TotalAmount = await CalculateSaleTotal(sale.Items, sale.StoreId, sagaId);
                var saleId = await _databaseService.CreateSale(sale);

                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "SaleService", "CreateSale", SagaEventType.Success, $"Sale created successfully with ID {saleId}", new { SaleId = saleId, sale.StoreId, sale.TotalAmount });
                }

                return saleId;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "SaleService", "CreateSale", SagaEventType.Failure, ex.Message, new { sale.StoreId, Error = ex.Message });
                }
                throw;
            }
        }

        public async Task<List<Sale>> GetRecentSales(string storeId, int limit = 10)
        {
            if (limit <= 0)
                throw new ArgumentException("Limit must be greater than zero", nameof(limit));
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));
            return await _databaseService.GetRecentSales(storeId, limit);
        }

        public async Task<Sale?> GetSaleById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Sale ID cannot be empty", nameof(id));

            return await _databaseService.GetSaleById(id);
        }

        public async Task<bool> CancelSale(string saleId, string storeId)
        {
            var sale = await GetSaleById(saleId);
            if (sale == null) return false;

            // Restore stock for each item
            foreach (var item in sale.Items)
            {
                await _productService.UpdateStock(item.ProductName, storeId, item.Quantity);
            }

            return await _databaseService.CancelSale(saleId);
        }

        public async Task<decimal> CalculateSaleTotal(List<SaleItem> items, string storeId, string? sagaId = null)
        {
            if (items == null || !items.Any())
                throw new ArgumentException("Items list cannot be empty", nameof(items));
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));

            try
            {
                // Simulate controlled failures
                await _failureService.SimulateNetworkTimeoutAsync("SaleService", sagaId);
                await _failureService.SimulateServiceUnavailableAsync("SaleService", sagaId);

                decimal total = 0;
                foreach (var item in items)
                {
                    var product = await _productService.GetProductByName(item.ProductName, storeId) ?? throw new InvalidOperationException($"Product {item.ProductName} not found");
                    total += product.Price * item.Quantity;
                }

                // Simulate payment failure for high amounts
                await _failureService.SimulatePaymentFailureAsync(total, "customer_001", sagaId);

                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "SaleService", "CalculateSaleTotal", SagaEventType.Success, $"Sale total calculated: {total:C}", new { StoreId = storeId, TotalAmount = total, ItemCount = items.Count });
                }

                return total;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "SaleService", "CalculateSaleTotal", SagaEventType.Failure, ex.Message, new { StoreId = storeId, Error = ex.Message });
                }
                throw;
            }
        }

        public async Task<bool> ValidateSaleItems(List<SaleItem> items, string storeId, string? sagaId = null)
        {
            if (items == null || !items.Any())
                return false;
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID cannot be empty", nameof(storeId));

            try
            {
                foreach (var item in items)
                {
                    if (!await _productService.ValidateProductExists(item.ProductName, storeId, sagaId))
                        return false;

                    if (!await _productService.ValidateStockAvailability(item.ProductName, storeId, item.Quantity, sagaId))
                        return false;
                }

                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "SaleService", "ValidateSaleItems", SagaEventType.Success, $"All sale items validated successfully", new { StoreId = storeId, ItemCount = items.Count });
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(sagaId))
                {
                    await _eventPublisher.PublishSagaEventAsync(sagaId, "SaleService", "ValidateSaleItems", SagaEventType.Failure, ex.Message, new { StoreId = storeId, Error = ex.Message });
                }
                throw;
            }
        }

        public async Task<bool> UpdateSale(Sale sale)
        {
            if (sale == null)
                throw new ArgumentNullException(nameof(sale));

            if (string.IsNullOrWhiteSpace(sale.Id))
                throw new ArgumentException("Sale ID cannot be empty", nameof(sale));

            // Verify the sale exists
            var existingSale = await GetSaleById(sale.Id);
            if (existingSale == null)
                return false;

            // Update the sale in the database
            return await _databaseService.UpdateSale(sale);
        }
    }
}
