using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CustomerService.Services;

public class CustomerService : ICustomerService
{
    private readonly IMongoCollection<Customer> _customers;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IMongoDatabase database, IDistributedCache cache, ILogger<CustomerService> logger)
    {
        _customers = database.GetCollection<Customer>("customers");
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        var cacheKey = "customers:all";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<IEnumerable<Customer>>(cached) ?? new List<Customer>();
        }

        var customers = await _customers.Find(c => c.IsActive).ToListAsync();
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(customers), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return customers;
    }

    public async Task<Customer?> GetCustomerByIdAsync(string id)
    {
        var cacheKey = $"customer:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<Customer>(cached);
        }

        var customer = await _customers.Find(c => c.Id == id && c.IsActive).FirstOrDefaultAsync();
        
        if (customer != null)
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(customer), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        }

        return customer;
    }

    public async Task<Customer?> GetCustomerByEmailAsync(string email)
    {
        var cacheKey = $"customer:email:{email}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<Customer>(cached);
        }

        var customer = await _customers.Find(c => c.Email == email && c.IsActive).FirstOrDefaultAsync();
        
        if (customer != null)
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(customer), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        }

        return customer;
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        // Check if email already exists
        var existingCustomer = await GetCustomerByEmailAsync(customer.Email);
        if (existingCustomer != null)
        {
            throw new InvalidOperationException($"Customer with email {customer.Email} already exists");
        }

        customer.Id = Guid.NewGuid().ToString();
        customer.RegistrationDate = DateTime.UtcNow;
        
        await _customers.InsertOneAsync(customer);
        
        // Invalidate related caches
        await InvalidateCustomerCaches();
        
        _logger.LogInformation("Customer created: {CustomerId}", customer.Id);
        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(string id, Customer customer)
    {
        customer.Id = id;
        
        var result = await _customers.ReplaceOneAsync(c => c.Id == id, customer);
        
        if (result.ModifiedCount > 0)
        {
            // Invalidate related caches
            await InvalidateCustomerCaches();
            await _cache.RemoveAsync($"customer:{id}");
            await _cache.RemoveAsync($"customer:email:{customer.Email}");
            
            _logger.LogInformation("Customer updated: {CustomerId}", id);
        }
        
        return customer;
    }

    public async Task<bool> DeleteCustomerAsync(string id)
    {
        var result = await _customers.DeleteOneAsync(c => c.Id == id);
        
        if (result.DeletedCount > 0)
        {
            // Invalidate related caches
            await InvalidateCustomerCaches();
            await _cache.RemoveAsync($"customer:{id}");
            
            _logger.LogInformation("Customer deleted: {CustomerId}", id);
            return true;
        }
        
        return false;
    }

    public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
    {
        var filter = Builders<Customer>.Filter.And(
            Builders<Customer>.Filter.Or(
                Builders<Customer>.Filter.Regex(c => c.FirstName, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                Builders<Customer>.Filter.Regex(c => c.LastName, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                Builders<Customer>.Filter.Regex(c => c.Email, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
            ),
            Builders<Customer>.Filter.Eq(c => c.IsActive, true)
        );
        
        return await _customers.Find(filter).ToListAsync();
    }

    public async Task<bool> DeactivateCustomerAsync(string id)
    {
        var update = Builders<Customer>.Update.Set(c => c.IsActive, false);
        var result = await _customers.UpdateOneAsync(c => c.Id == id, update);
        
        if (result.ModifiedCount > 0)
        {
            await InvalidateCustomerCaches();
            await _cache.RemoveAsync($"customer:{id}");
            
            _logger.LogInformation("Customer deactivated: {CustomerId}", id);
            return true;
        }
        
        return false;
    }

    public async Task<bool> ActivateCustomerAsync(string id)
    {
        var update = Builders<Customer>.Update.Set(c => c.IsActive, true);
        var result = await _customers.UpdateOneAsync(c => c.Id == id, update);
        
        if (result.ModifiedCount > 0)
        {
            await InvalidateCustomerCaches();
            await _cache.RemoveAsync($"customer:{id}");
            
            _logger.LogInformation("Customer activated: {CustomerId}", id);
            return true;
        }
        
        return false;
    }

    public async Task<IEnumerable<Customer>> GetCustomersByStoreAsync(string storeId)
    {
        return await _customers.Find(c => c.PreferredStoreId == storeId && c.IsActive).ToListAsync();
    }

    public async Task<bool> UpdateCustomerStatsAsync(string customerId, int orderCount, decimal totalSpent)
    {
        var update = Builders<Customer>.Update
            .Set(c => c.TotalOrders, orderCount)
            .Set(c => c.TotalSpent, totalSpent);
        
        var result = await _customers.UpdateOneAsync(c => c.Id == customerId, update);
        
        if (result.ModifiedCount > 0)
        {
            await _cache.RemoveAsync($"customer:{customerId}");
            _logger.LogInformation("Customer stats updated: {CustomerId}", customerId);
            return true;
        }
        
        return false;
    }

    private async Task InvalidateCustomerCaches()
    {
        await _cache.RemoveAsync("customers:all");
        // Note: In a production environment, you might want to implement a more sophisticated cache invalidation strategy
    }
} 