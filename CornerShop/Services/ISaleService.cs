using CornerShop.Models;

namespace CornerShop.Services
{
    public interface ISaleService
    {
        Task<string> CreateSale(Sale sale, string? sagaId = null);
        Task<List<Sale>> GetRecentSales(string storeId, int limit = 10);
        Task<Sale?> GetSaleById(string id);
        Task<bool> CancelSale(string saleId, string storeId);
        Task<decimal> CalculateSaleTotal(List<SaleItem> items, string storeId, string? sagaId = null);
        Task<bool> ValidateSaleItems(List<SaleItem> items, string storeId, string? sagaId = null);
        Task<bool> UpdateSale(Sale sale);
    }
}
