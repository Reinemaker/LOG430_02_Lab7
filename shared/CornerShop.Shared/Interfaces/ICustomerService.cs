using CornerShop.Shared.Models;

namespace CornerShop.Shared.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(string id);
    Task<Customer?> GetCustomerByEmailAsync(string email);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(string id, Customer customer);
    Task<bool> DeleteCustomerAsync(string id);
    Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
    Task<bool> DeactivateCustomerAsync(string id);
    Task<bool> ActivateCustomerAsync(string id);
    Task<IEnumerable<Customer>> GetCustomersByStoreAsync(string storeId);
    Task<bool> UpdateCustomerStatsAsync(string customerId, int orderCount, decimal totalSpent);
}
