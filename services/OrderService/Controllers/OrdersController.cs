using Microsoft.AspNetCore.Mvc;
using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ISagaParticipant _sagaParticipant;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ISagaParticipant sagaParticipant, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _sagaParticipant = sagaParticipant;
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
            _logger.LogError(ex, "Error retrieving orders by date range {StartDate} to {EndDate}", startDate, endDate);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(Order order)
    {
        try
        {
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
            var order = new Order
            {
                CustomerId = request.CustomerId,
                StoreId = request.StoreId,
                PaymentMethod = request.PaymentMethod,
                ShippingAddress = request.ShippingAddress,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdOrder = await _orderService.CreateOrderAsync(order);
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing checkout");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("choreographed-saga")]
    public async Task<ActionResult<Order>> CreateOrderWithChoreographedSaga(ChoreographedSagaOrderRequest request)
    {
        try
        {
            var order = new Order
            {
                CustomerId = request.CustomerId,
                StoreId = request.StoreId,
                PaymentMethod = request.PaymentMethod,
                ShippingAddress = request.ShippingAddress,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdOrder = await _orderService.CreateOrderAsync(order);

            // Publish OrderCreated event to start the choreographed saga
            var orderCreatedEvent = new OrderCreatedEvent(
                createdOrder.Id,
                request.CustomerId,
                request.TotalAmount,
                request.Items
            );

            // Note: In a real implementation, you would inject IEventProducer and publish the event
            // await _eventProducer.PublishAsync(orderCreatedEvent, "sagas.events");

            _logger.LogInformation("Order created with choreographed saga: {OrderId}", createdOrder.Id);
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order with choreographed saga");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<Order>> UpdateOrderStatus(string id, [FromBody] OrderStatus status)
    {
        try
        {
            var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, status);
            if (updatedOrder == null)
            {
                return NotFound();
            }
            return Ok(updatedOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status {OrderId} to {Status}", id, status);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("{id}/payment-status")]
    public async Task<ActionResult<Order>> UpdatePaymentStatus(string id, [FromBody] PaymentStatus status)
    {
        try
        {
            var updatedOrder = await _orderService.UpdatePaymentStatusAsync(id, status);
            if (updatedOrder == null)
            {
                return NotFound();
            }
            return Ok(updatedOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status {OrderId} to {Status}", id, status);
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

    // Saga Participation Endpoints

    /// <summary>
    /// Participate in saga orchestration
    /// </summary>
    [HttpPost("saga/participate")]
    [ProducesResponseType(typeof(SagaParticipantResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> ParticipateInSaga([FromBody] SagaParticipantRequest request)
    {
        try
        {
            _logger.LogInformation("Received saga participation request: {SagaId} | {StepName} | {OrderId}",
                request.SagaId, request.StepName, request.OrderId);

            var response = await _sagaParticipant.ExecuteStepAsync(request);

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                status = 200,
                data = response,
                links = new
                {
                    self = Url.Action(nameof(ParticipateInSaga), "Orders", null, Request.Scheme),
                    compensate = Url.Action(nameof(CompensateSagaStep), "Orders", new { sagaId = request.SagaId, stepName = request.StepName }, Request.Scheme)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga participation failed: {SagaId} | {StepName}", request.SagaId, request.StepName);
            return StatusCode(500, new
            {
                timestamp = DateTime.UtcNow,
                status = 500,
                error = "Internal Server Error",
                message = ex.Message,
                path = Request.Path
            });
        }
    }

    /// <summary>
    /// Compensate a saga step
    /// </summary>
    [HttpPost("saga/compensate")]
    [ProducesResponseType(typeof(SagaCompensationResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<IActionResult> CompensateSagaStep([FromBody] SagaCompensationRequest request)
    {
        try
        {
            _logger.LogInformation("Received saga compensation request: {SagaId} | {StepName} | Reason: {Reason}",
                request.SagaId, request.StepName, request.Reason);

            var response = await _sagaParticipant.CompensateStepAsync(request);

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                status = 200,
                data = response,
                links = new
                {
                    self = Url.Action(nameof(CompensateSagaStep), "Orders", null, Request.Scheme),
                    participate = Url.Action(nameof(ParticipateInSaga), "Orders", null, Request.Scheme)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga compensation failed: {SagaId} | {StepName}", request.SagaId, request.StepName);
            return StatusCode(500, new
            {
                timestamp = DateTime.UtcNow,
                status = 500,
                error = "Internal Server Error",
                message = ex.Message,
                path = Request.Path
            });
        }
    }

    /// <summary>
    /// Get saga participant information
    /// </summary>
    [HttpGet("saga/info")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetSagaInfo()
    {
        try
        {
            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                status = 200,
                data = new
                {
                    serviceName = _sagaParticipant.ServiceName,
                    supportedSteps = _sagaParticipant.SupportedSteps,
                    description = "Order Service - Handles order creation, confirmation, and cancellation"
                },
                links = new
                {
                    self = Url.Action(nameof(GetSagaInfo), "Orders", null, Request.Scheme),
                    participate = Url.Action(nameof(ParticipateInSaga), "Orders", null, Request.Scheme),
                    compensate = Url.Action(nameof(CompensateSagaStep), "Orders", null, Request.Scheme)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get saga info");
            return StatusCode(500, new
            {
                timestamp = DateTime.UtcNow,
                status = 500,
                error = "Internal Server Error",
                message = ex.Message,
                path = Request.Path
            });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                status = 200,
                data = new
                {
                    service = "OrderService",
                    status = "Healthy",
                    uptime = Environment.TickCount64,
                    sagaParticipant = _sagaParticipant.ServiceName
                },
                links = new
                {
                    self = Url.Action(nameof(GetHealth), "Orders", null, Request.Scheme),
                    sagaInfo = Url.Action(nameof(GetSagaInfo), "Orders", null, Request.Scheme)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new
            {
                timestamp = DateTime.UtcNow,
                status = 500,
                error = "Service Unhealthy",
                message = ex.Message,
                path = Request.Path
            });
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

public class ChoreographedSagaOrderRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public Address? ShippingAddress { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
