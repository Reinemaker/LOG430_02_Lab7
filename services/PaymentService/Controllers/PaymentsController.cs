using Microsoft.AspNetCore.Mvc;
using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly ISagaParticipant _sagaParticipant;
    private readonly IEventProducer _eventProducer;

    public PaymentsController(
        ILogger<PaymentsController> logger,
        ISagaParticipant sagaParticipant,
        IEventProducer eventProducer)
    {
        _logger = logger;
        _sagaParticipant = sagaParticipant;
        _eventProducer = eventProducer;
    }

    [HttpGet]
    public ActionResult<string> GetPayments()
    {
        return Ok("Payment Service - Saga Participant Implementation");
    }

    [HttpGet("health")]
    public ActionResult<string> Health()
    {
        return Ok("Payment Service is healthy");
    }

    [HttpPost("saga/participate")]
    public async Task<ActionResult<SagaParticipantResponse>> ParticipateInSaga([FromBody] SagaParticipantRequest request)
    {
        try
        {
            _logger.LogInformation("Participating in saga step {StepName} for correlation {CorrelationId}", 
                request.StepName, request.CorrelationId);

            var response = await _sagaParticipant.ExecuteStepAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error participating in saga step {StepName}", request.StepName);
            return StatusCode(500, new SagaParticipantResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
    }

    [HttpPost("saga/compensate")]
    public async Task<ActionResult<SagaCompensationResponse>> CompensateSagaStep([FromBody] SagaCompensationRequest request)
    {
        try
        {
            _logger.LogInformation("Compensating saga step {StepName} for correlation {CorrelationId}", 
                request.StepName, request.CorrelationId);

            var response = await _sagaParticipant.CompensateStepAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating saga step {StepName}", request.StepName);
            return StatusCode(500, new SagaCompensationResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
    }

    [HttpGet("saga/info")]
    public ActionResult<object> GetSagaInfo()
    {
        return Ok(new
        {
            ServiceName = _sagaParticipant.ServiceName,
            SupportedSteps = _sagaParticipant.SupportedSteps,
            Description = "Payment Service handles payment processing for order completion"
        });
    }

    [HttpGet("events/statistics")]
    public async Task<ActionResult<EventStatistics>> GetEventStatistics()
    {
        try
        {
            var statistics = await _eventProducer.GetEventStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event statistics");
            return StatusCode(500, "Error retrieving event statistics");
        }
    }

    [HttpPost("process")]
    public async Task<ActionResult<object>> ProcessPayment([FromBody] PaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing payment for customer {CustomerId} amount {Amount}", 
                request.CustomerId, request.Amount);

            var sagaRequest = new SagaParticipantRequest
            {
                StepName = "ProcessPayment",
                CorrelationId = Guid.NewGuid().ToString(),
                Data = new Dictionary<string, object>
                {
                    ["customerId"] = request.CustomerId,
                    ["amount"] = request.Amount,
                    ["paymentMethod"] = request.PaymentMethod
                }
            };

            var response = await _sagaParticipant.ExecuteStepAsync(sagaRequest);
            
            if (response.Success)
            {
                return Ok(new
                {
                    Success = true,
                    TransactionId = response.Data?.GetType().GetProperty("TransactionId")?.GetValue(response.Data),
                    Amount = request.Amount,
                    Message = "Payment processed successfully"
                });
            }
            else
            {
                return BadRequest(new
                {
                    Success = false,
                    Error = response.ErrorMessage,
                    Message = "Payment processing failed"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return StatusCode(500, "Internal server error during payment processing");
        }
    }
}

public class PaymentRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "CreditCard";
} 