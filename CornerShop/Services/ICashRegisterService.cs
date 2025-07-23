using CornerShop.Models;

namespace CornerShop.Services
{
    public interface ICashRegisterService
    {
        Task<bool> LockRegister(int registerId);
        Task<bool> UnlockRegister(int registerId);
        Task<bool> IsRegisterLocked(int registerId);
        Task<bool> ValidateRegisterAccess(int registerId);
        Task<Sale> CreateSaleOnRegister(int registerId, Sale sale);
        Task<bool> CancelSaleOnRegister(int registerId, string saleId);
    }
}
