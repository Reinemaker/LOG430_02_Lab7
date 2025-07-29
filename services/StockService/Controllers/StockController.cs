using Microsoft.AspNetCore.Mvc;
using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;

namespace StockService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly ILogger<StockController> _logger;
    private readonly ISagaParticipant _sagaParticipant;
    private readonly IEventProducer _eventProducer;

    public StockController(
        ILogger<StockController> logger,
        ISagaParticipant sagaParticipant,
        IEventProducer eventProducer)
    {
        _logger = logger;
        _sagaParticipant = sagaParticipant;
        _eventProducer = eventProducer;
    }

    [HttpGet]
    public ActionResult<string> GetStock()
    {
        return Ok("Stock Service - Saga Participant Implementation");
    }

    [HttpGet("health")]
    public ActionResult<string> Health()
    {
        return Ok("Stock Service is healthy");
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
            _sagaParticipant.ServiceName,
            _sagaParticipant.SupportedSteps,
            Description = "Stock Service handles stock verification and reservation for order processing"
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
}
