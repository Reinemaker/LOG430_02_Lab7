using Microsoft.AspNetCore.Mvc;
using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(string id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("number/{orderNumber}")]
    public async Task<ActionResult<Order>> GetOrderByNumber(string orderNumber)
    {
        try
        {
            var order = await _orderService.GetOrderByOrderNumberAsync(orderNumber);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order by number {OrderNumber}", orderNumber);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByCustomer(string customerId)
    {
        try
        {
            var orders = await _orderService.GetOrdersByCustomerAsync(customerId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("store/{storeId}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByStore(string storeId)
    {
        try
        {
            var orders = await _orderService.GetOrdersByStoreAsync(storeId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for store {StoreId}", storeId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByStatus(OrderStatus status)
    {
        try
        {
            var orders = await _orderService.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders by status {Status}", status);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("date-range")]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var orders = await _orderService.GetOrdersByDateRangeAsync(startDate, endDate);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders by date range");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(Order order)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _orderService.ValidateOrderAsync(order);
            if (!isValid)
            {
                return BadRequest("Invalid order data");
            }

            var createdOrder = await _orderService.CreateOrderAsync(order);
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<Order>> ProcessCheckout(CheckoutRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var order = await _orderService.ProcessCheckoutAsync(
                request.CustomerId, 
                request.StoreId, 
                request.PaymentMethod, 
                request.ShippingAddress);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing checkout");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<Order>> UpdateOrderStatus(string id, [FromBody] OrderStatus status)
    {
        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(id, status);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status {OrderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/payment-status")]
    public async Task<ActionResult<Order>> UpdatePaymentStatus(string id, [FromBody] PaymentStatus status)
    {
        try
        {
            var order = await _orderService.UpdatePaymentStatusAsync(id, status);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status {OrderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/cancel")]
    public async Task<ActionResult> CancelOrder(string id)
    {
        try
        {
            var success = await _orderService.CancelOrderAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOrder(string id)
    {
        try
        {
            var success = await _orderService.DeleteOrderAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class CheckoutRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public Address? ShippingAddress { get; set; }
} 