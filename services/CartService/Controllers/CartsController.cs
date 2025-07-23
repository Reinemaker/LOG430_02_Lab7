using Microsoft.AspNetCore.Mvc;
using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;

namespace CartService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartsController> _logger;

    public CartsController(ICartService cartService, ILogger<CartsController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    [HttpGet("{customerId}")]
    public async Task<ActionResult<Cart>> GetCart(string customerId)
    {
        try
        {
            var cart = await _cartService.GetCartByCustomerIdAsync(customerId);
            if (cart == null)
            {
                return NotFound();
            }
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{customerId}")]
    public async Task<ActionResult<Cart>> CreateCart(string customerId)
    {
        try
        {
            var cart = await _cartService.CreateCartAsync(customerId);
            return CreatedAtAction(nameof(GetCart), new { customerId }, cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cart for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{customerId}/items")]
    public async Task<ActionResult<Cart>> AddItemToCart(string customerId, [FromBody] CartItem item)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cart = await _cartService.AddItemToCartAsync(customerId, item);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{customerId}/items/{productId}")]
    public async Task<ActionResult<Cart>> UpdateCartItem(string customerId, string productId, [FromBody] int quantity)
    {
        try
        {
            if (quantity < 0)
            {
                return BadRequest("Quantity cannot be negative");
            }

            var cart = await _cartService.UpdateCartItemAsync(customerId, productId, quantity);
            return Ok(cart);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{customerId}/items/{productId}")]
    public async Task<ActionResult<Cart>> RemoveItemFromCart(string customerId, string productId)
    {
        try
        {
            var cart = await _cartService.RemoveItemFromCartAsync(customerId, productId);
            return Ok(cart);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{customerId}/clear")]
    public async Task<ActionResult> ClearCart(string customerId)
    {
        try
        {
            var success = await _cartService.ClearCartAsync(customerId);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{customerId}")]
    public async Task<ActionResult> DeleteCart(string customerId)
    {
        try
        {
            var success = await _cartService.DeleteCartAsync(customerId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cart for customer {CustomerId}", customerId);
            return StatusCode(500, "Internal server error");
        }
    }
} 