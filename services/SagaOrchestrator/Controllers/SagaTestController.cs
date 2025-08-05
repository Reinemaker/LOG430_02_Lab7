using Microsoft.AspNetCore.Mvc;
using SagaOrchestrator.Services;
using CornerShop.Shared.Events;
using CornerShop.Shared.Models;
using System.Text.Json;

namespace SagaOrchestrator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SagaTestController : ControllerBase
{
    private readonly ISagaOrchestratorService _sagaOrchestrator;
    private readonly ILogger<SagaTestController> _logger;

    public SagaTestController(ISagaOrchestratorService sagaOrchestrator, ILogger<SagaTestController> logger)
    {
        _sagaOrchestrator = sagaOrchestrator;
        _logger = logger;
    }

    [HttpPost("start-success-saga")]
    public async Task<ActionResult<object>> StartSuccessSaga([FromBody] StartSagaRequest request)
    {
        try
        {
            var orderId = request.OrderId ?? Guid.NewGuid().ToString();
            var customerId = request.CustomerId ?? Guid.NewGuid().ToString();
            var totalAmount = request.TotalAmount ?? 150.00m;
            var items = request.Items ?? new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = "prod-1",
                    ProductName = "Test Product 1",
                    Quantity = 2,
                    UnitPrice = 50.00m,
                    TotalPrice = 100.00m
                },
                new OrderItem
                {
                    ProductId = "prod-2",
                    ProductName = "Test Product 2",
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    TotalPrice = 50.00m
                }
            };

            await _sagaOrchestrator.StartOrderSagaAsync(orderId, customerId, totalAmount, items);

            return Ok(new
            {
                Message = "Success Saga started",
                OrderId = orderId,
                CustomerId = customerId,
                TotalAmount = totalAmount,
                Items = items,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start success saga");
            return StatusCode(500, new { Error = "Failed to start saga", ex.Message });
        }
    }

    [HttpPost("start-failure-saga")]
    public async Task<ActionResult<object>> StartFailureSaga([FromBody] StartSagaRequest request)
    {
        try
        {
            var orderId = request.OrderId ?? Guid.NewGuid().ToString();
            var customerId = request.CustomerId ?? Guid.NewGuid().ToString();
            var totalAmount = request.TotalAmount ?? 150.00m;
            var items = request.Items ?? new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = "prod-fail",
                    ProductName = "Failing Product",
                    Quantity = 1,
                    UnitPrice = 150.00m,
                    TotalPrice = 150.00m
                }
            };

            await _sagaOrchestrator.StartOrderSagaAsync(orderId, customerId, totalAmount, items);

            return Ok(new
            {
                Message = "Failure Saga started (will fail at payment step)",
                OrderId = orderId,
                CustomerId = customerId,
                TotalAmount = totalAmount,
                Items = items,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start failure saga");
            return StatusCode(500, new { Error = "Failed to start saga", ex.Message });
        }
    }

    [HttpPost("simulate-step-completion")]
    public async Task<ActionResult<object>> SimulateStepCompletion([FromBody] SimulateStepRequest request)
    {
        try
        {
            var stepData = new
            {
                request.StepName,
                CompletedAt = DateTime.UtcNow,
                Data = request.StepData
            };

            await _sagaOrchestrator.HandleSagaStepCompletedAsync(
                request.SagaId,
                request.StepName,
                request.StepNumber,
                stepData);

            return Ok(new
            {
                Message = "Step completion simulated",
                request.SagaId,
                request.StepName,
                request.StepNumber,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate step completion");
            return StatusCode(500, new { Error = "Failed to simulate step", ex.Message });
        }
    }

    [HttpPost("simulate-step-failure")]
    public async Task<ActionResult<object>> SimulateStepFailure([FromBody] SimulateStepFailureRequest request)
    {
        try
        {
            var stepData = new
            {
                request.StepName,
                FailedAt = DateTime.UtcNow,
                request.ErrorMessage
            };

            await _sagaOrchestrator.HandleSagaStepFailedAsync(
                request.SagaId,
                request.StepName,
                request.StepNumber,
                request.ErrorMessage,
                stepData);

            return Ok(new
            {
                Message = "Step failure simulated",
                request.SagaId,
                request.StepName,
                request.StepNumber,
                request.ErrorMessage,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate step failure");
            return StatusCode(500, new { Error = "Failed to simulate failure", ex.Message });
        }
    }

    [HttpPost("simulate-compensation-completion")]
    public async Task<ActionResult<object>> SimulateCompensationCompletion([FromBody] SimulateCompensationRequest request)
    {
        try
        {
            await _sagaOrchestrator.HandleSagaCompensationCompletedAsync(
                request.SagaId,
                request.StepName,
                request.StepNumber);

            return Ok(new
            {
                Message = "Compensation completion simulated",
                request.SagaId,
                request.StepName,
                request.StepNumber,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate compensation completion");
            return StatusCode(500, new { Error = "Failed to simulate compensation", ex.Message });
        }
    }

    [HttpGet("saga-state/{sagaId}")]
    public async Task<ActionResult<SagaState>> GetSagaState(string sagaId)
    {
        try
        {
            var sagaState = await _sagaOrchestrator.GetSagaStateAsync(sagaId);
            if (sagaState == null)
            {
                return NotFound(new { Error = "Saga not found", SagaId = sagaId });
            }

            return Ok(sagaState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get saga state");
            return StatusCode(500, new { Error = "Failed to get saga state", ex.Message });
        }
    }

    [HttpGet("saga-states/order/{orderId}")]
    public async Task<ActionResult<List<SagaState>>> GetSagaStatesByOrderId(string orderId)
    {
        try
        {
            var sagaStates = await _sagaOrchestrator.GetSagaStatesByOrderIdAsync(orderId);
            return Ok(sagaStates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get saga states by order ID");
            return StatusCode(500, new { Error = "Failed to get saga states", ex.Message });
        }
    }

    [HttpPost("run-complete-success-scenario")]
    public async Task<ActionResult<object>> RunCompleteSuccessScenario()
    {
        try
        {
            var orderId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            var totalAmount = 200.00m;
            var items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = "prod-success-1",
                    ProductName = "Success Product 1",
                    Quantity = 2,
                    UnitPrice = 75.00m,
                    TotalPrice = 150.00m
                },
                new OrderItem
                {
                    ProductId = "prod-success-2",
                    ProductName = "Success Product 2",
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    TotalPrice = 50.00m
                }
            };

            // Start saga
            await _sagaOrchestrator.StartOrderSagaAsync(orderId, customerId, totalAmount, items);

            // Simulate successful completion of all steps
            var sagaId = Guid.NewGuid().ToString(); // In real scenario, this would be returned from StartOrderSagaAsync

            await Task.Delay(1000); // Simulate processing time

            // Simulate step completions
            await _sagaOrchestrator.HandleSagaStepCompletedAsync(sagaId, "CreateOrder", 1, new { OrderId = orderId });
            await Task.Delay(500);
            await _sagaOrchestrator.HandleSagaStepCompletedAsync(sagaId, "ReserveStock", 2, new { ReservedItems = items });
            await Task.Delay(500);
            await _sagaOrchestrator.HandleSagaStepCompletedAsync(sagaId, "ProcessPayment", 3, new { PaymentId = Guid.NewGuid().ToString() });
            await Task.Delay(500);
            await _sagaOrchestrator.HandleSagaStepCompletedAsync(sagaId, "ConfirmOrder", 4, new { ConfirmedAt = DateTime.UtcNow });
            await Task.Delay(500);
            await _sagaOrchestrator.HandleSagaStepCompletedAsync(sagaId, "SendNotifications", 5, new { NotificationsSent = true });

            return Ok(new
            {
                Message = "Complete success scenario executed",
                OrderId = orderId,
                SagaId = sagaId,
                TotalAmount = totalAmount,
                Status = "Completed",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run complete success scenario");
            return StatusCode(500, new { Error = "Failed to run scenario", ex.Message });
        }
    }

    [HttpPost("run-complete-failure-scenario")]
    public async Task<ActionResult<object>> RunCompleteFailureScenario()
    {
        try
        {
            var orderId = Guid.NewGuid().ToString();
            var customerId = Guid.NewGuid().ToString();
            var totalAmount = 300.00m;
            var items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = "prod-fail-1",
                    ProductName = "Failing Product 1",
                    Quantity = 1,
                    UnitPrice = 300.00m,
                    TotalPrice = 300.00m
                }
            };

            // Start saga
            await _sagaOrchestrator.StartOrderSagaAsync(orderId, customerId, totalAmount, items);

            // Simulate failure scenario
            var sagaId = Guid.NewGuid().ToString(); // In real scenario, this would be returned from StartOrderSagaAsync

            await Task.Delay(1000); // Simulate processing time

            // Simulate successful steps
            await _sagaOrchestrator.HandleSagaStepCompletedAsync(sagaId, "CreateOrder", 1, new { OrderId = orderId });
            await Task.Delay(500);
            await _sagaOrchestrator.HandleSagaStepCompletedAsync(sagaId, "ReserveStock", 2, new { ReservedItems = items });
            await Task.Delay(500);

            // Simulate failure at payment step
            await _sagaOrchestrator.HandleSagaStepFailedAsync(sagaId, "ProcessPayment", 3, "Payment processing failed - insufficient funds", new { PaymentAttempted = true });

            // Simulate compensation completions
            await Task.Delay(1000);
            await _sagaOrchestrator.HandleSagaCompensationCompletedAsync(sagaId, "ReserveStock", 2);
            await Task.Delay(500);
            await _sagaOrchestrator.HandleSagaCompensationCompletedAsync(sagaId, "CreateOrder", 1);

            return Ok(new
            {
                Message = "Complete failure scenario executed",
                OrderId = orderId,
                SagaId = sagaId,
                TotalAmount = totalAmount,
                Status = "Failed",
                FailureReason = "Payment processing failed",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run complete failure scenario");
            return StatusCode(500, new { Error = "Failed to run scenario", ex.Message });
        }
    }
}

public class StartSagaRequest
{
    public string? OrderId { get; set; }
    public string? CustomerId { get; set; }
    public decimal? TotalAmount { get; set; }
    public List<OrderItem>? Items { get; set; }
}

public class SimulateStepRequest
{
    public string SagaId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public object? StepData { get; set; }
}

public class SimulateStepFailureRequest
{
    public string SagaId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class SimulateCompensationRequest
{
    public string SagaId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public int StepNumber { get; set; }
}
