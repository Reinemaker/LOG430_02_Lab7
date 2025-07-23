using CornerShop.Models;

namespace CornerShop.Services
{
    public class CashRegisterService : ICashRegisterService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IProductService _productService;
        private readonly ISaleService _saleService;
        private static readonly SemaphoreSlim[] _registerLocks = new SemaphoreSlim[3];
        private static readonly Dictionary<int, string> _activeSales = new();

        public CashRegisterService(
            IDatabaseService databaseService,
            IProductService productService,
            ISaleService saleService)
        {
            _databaseService = databaseService;
            _productService = productService;
            _saleService = saleService;

            // Initialize locks for each register
            for (int i = 0; i < 3; i++)
            {
                _registerLocks[i] = new SemaphoreSlim(1, 1);
            }
        }

        public async Task<bool> LockRegister(int registerId)
        {
            if (registerId < 0 || registerId >= 3)
                throw new ArgumentException("Invalid register ID", nameof(registerId));

            return await _registerLocks[registerId].WaitAsync(TimeSpan.FromSeconds(5));
        }

        public Task<bool> UnlockRegister(int registerId)
        {
            if (registerId < 0 || registerId >= 3)
                throw new ArgumentException("Invalid register ID", nameof(registerId));

            _registerLocks[registerId].Release();
            return Task.FromResult(true);
        }

        public async Task<bool> IsRegisterLocked(int registerId)
        {
            if (registerId < 0 || registerId >= 3)
                throw new ArgumentException("Invalid register ID", nameof(registerId));

            return !await _registerLocks[registerId].WaitAsync(0);
        }

        public async Task<bool> ValidateRegisterAccess(int registerId)
        {
            if (registerId < 0 || registerId >= 3)
                throw new ArgumentException("Invalid register ID", nameof(registerId));

            return await LockRegister(registerId);
        }

        public async Task<Sale> CreateSaleOnRegister(int registerId, Sale sale)
        {
            if (!await ValidateRegisterAccess(registerId))
                throw new InvalidOperationException($"Register {registerId} is currently in use");

            try
            {
                // Validate stock availability for all items
                foreach (var item in sale.Items)
                {
                    if (!await _productService.ValidateStockAvailability(item.ProductName, sale.StoreId, item.Quantity))
                    {
                        throw new InvalidOperationException($"Insufficient stock for {item.ProductName}");
                    }
                }

                // Create sale and update stock atomically
                var saleId = await _saleService.CreateSale(sale);
                _activeSales[registerId] = saleId;

                return sale;
            }
            catch (Exception)
            {
                // If anything fails, ensure the register is unlocked
                await UnlockRegister(registerId);
                throw;
            }
        }

        public async Task<bool> CancelSaleOnRegister(int registerId, string saleId)
        {
            if (!await ValidateRegisterAccess(registerId))
                throw new InvalidOperationException($"Register {registerId} is currently in use");

            try
            {
                if (_activeSales.TryGetValue(registerId, out var activeSaleId) && activeSaleId == saleId)
                {
                    // Get the store ID from the active sale
                    var activeSale = await _saleService.GetSaleById(saleId);
                    if (activeSale == null) return false;

                    var success = await _saleService.CancelSale(saleId, activeSale.StoreId);
                    if (success)
                    {
                        _activeSales.Remove(registerId);
                    }
                    return success;
                }
                return false;
            }
            finally
            {
                await UnlockRegister(registerId);
            }
        }
    }
}
