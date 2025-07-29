using Microsoft.AspNetCore.Mvc;
using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;

namespace CustomerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
    {
        try
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(string id)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("email/{email}")]
    public async Task<ActionResult<Customer>> GetCustomerByEmail(string email)
    {
        try
        {
            var customer = await _customerService.GetCustomerByEmailAsync(email);
            if (customer == null)
            {
                return NotFound();
            }
            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer by email {Email}", email);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Customer>>> SearchCustomers([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Search term is required");
            }

            var customers = await _customerService.SearchCustomersAsync(q);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers with term {SearchTerm}", q);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("store/{storeId}")]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomersByStore(string storeId)
    {
        try
        {
            var customers = await _customerService.GetCustomersByStoreAsync(storeId);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers for store {StoreId}", storeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdCustomer = await _customerService.CreateCustomerAsync(customer);
            return CreatedAtAction(nameof(GetCustomer), new { id = createdCustomer.Id }, createdCustomer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Customer>> UpdateCustomer(string id, Customer customer)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedCustomer = await _customerService.UpdateCustomerAsync(id, customer);
            return Ok(updatedCustomer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult> DeactivateCustomer(string id)
    {
        try
        {
            var success = await _customerService.DeactivateCustomerAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating customer {CustomerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/activate")]
    public async Task<ActionResult> ActivateCustomer(string id)
    {
        try
        {
            var success = await _customerService.ActivateCustomerAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating customer {CustomerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/stats")]
    public async Task<ActionResult> UpdateCustomerStats(string id, [FromBody] CustomerStatsRequest request)
    {
        try
        {
            var success = await _customerService.UpdateCustomerStatsAsync(id, request.OrderCount, request.TotalSpent);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer stats {CustomerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCustomer(string id)
    {
        try
        {
            var success = await _customerService.DeleteCustomerAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class CustomerStatsRequest
{
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}
