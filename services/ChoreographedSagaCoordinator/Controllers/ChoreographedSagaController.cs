using Microsoft.AspNetCore.Mvc;
using ChoreographedSagaCoordinator.Services;
using CornerShop.Shared.Models;

namespace ChoreographedSagaCoordinator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChoreographedSagaController : ControllerBase
{
    private readonly IChoreographedSagaCoordinator _sagaCoordinator;
    private readonly IChoreographedSagaStateManager _stateManager;
    private readonly ILogger<ChoreographedSagaController> _logger;

    public ChoreographedSagaController(
        IChoreographedSagaCoordinator sagaCoordinator,
        IChoreographedSagaStateManager stateManager,
        ILogger<ChoreographedSagaController> logger)
    {
        _sagaCoordinator = sagaCoordinator;
        _stateManager = stateManager;
        _logger = logger;
    }

    [HttpGet("state/{sagaId}")]
    [ProducesResponseType(typeof(ChoreographedSagaState), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<IActionResult> GetSagaState(string sagaId)
    {
        try
        {
            var sagaState = await _sagaCoordinator.GetSagaStateAsync(sagaId);
            if (sagaState == null)
            {
                return NotFound(new { message = $"Saga with ID {sagaId} not found" });
            }

            return Ok(sagaState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga state for Saga: {SagaId}", sagaId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("states")]
    [ProducesResponseType(typeof(List<ChoreographedSagaState>), 200)]
    public async Task<IActionResult> GetAllSagaStates()
    {
        try
        {
            var sagaStates = await _sagaCoordinator.GetAllSagaStatesAsync();
            return Ok(sagaStates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all saga states");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("states/status/{status}")]
    [ProducesResponseType(typeof(List<ChoreographedSagaState>), 200)]
    public async Task<IActionResult> GetSagaStatesByStatus(ChoreographedSagaStatus status)
    {
        try
        {
            var sagaStates = await _stateManager.GetSagaStatesByStatusAsync(status);
            return Ok(sagaStates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga states by status: {Status}", status);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("states/business-process/{businessProcess}")]
    [ProducesResponseType(typeof(List<ChoreographedSagaState>), 200)]
    public async Task<IActionResult> GetSagaStatesByBusinessProcess(string businessProcess)
    {
        try
        {
            var sagaStates = await _stateManager.GetSagaStatesByBusinessProcessAsync(businessProcess);
            return Ok(sagaStates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga states by business process: {BusinessProcess}", businessProcess);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("states/date-range")]
    [ProducesResponseType(typeof(List<ChoreographedSagaState>), 200)]
    public async Task<IActionResult> GetSagaStatesByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var sagaStates = await _stateManager.GetSagaStatesByDateRangeAsync(startDate, endDate);
            return Ok(sagaStates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga states by date range: {StartDate} to {EndDate}", startDate, endDate);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ChoreographedSagaStatistics), 200)]
    public async Task<IActionResult> GetSagaStatistics()
    {
        try
        {
            var statistics = await _stateManager.GetSagaStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("state/{sagaId}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<IActionResult> DeleteSagaState(string sagaId)
    {
        try
        {
            var sagaState = await _sagaCoordinator.GetSagaStateAsync(sagaId);
            if (sagaState == null)
            {
                return NotFound(new { message = $"Saga with ID {sagaId} not found" });
            }

            await _stateManager.DeleteSagaStateAsync(sagaId);
            return Ok(new { message = $"Saga state {sagaId} deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saga state for Saga: {SagaId}", sagaId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var healthInfo = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "ChoreographedSagaCoordinator",
                Version = "1.0.0",
                Features = new[]
                {
                    "Choreographed Saga Coordination",
                    "Event-Driven Architecture",
                    "Compensation Handling",
                    "State Management",
                    "Metrics Collection"
                }
            };

            return Ok(healthInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health information");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var statistics = await _stateManager.GetSagaStatisticsAsync();
            
            var metrics = new
            {
                Timestamp = DateTime.UtcNow,
                TotalSagas = statistics.TotalSagas,
                CompletedSagas = statistics.CompletedSagas,
                FailedSagas = statistics.FailedSagas,
                InProgressSagas = statistics.InProgressSagas,
                CompensatedSagas = statistics.CompensatedSagas,
                AverageDurationSeconds = statistics.AverageDurationSeconds,
                SuccessRate = statistics.TotalSagas > 0 ? (double)statistics.CompletedSagas / statistics.TotalSagas * 100 : 0,
                FailureRate = statistics.TotalSagas > 0 ? (double)statistics.FailedSagas / statistics.TotalSagas * 100 : 0,
                CompensationRate = statistics.TotalSagas > 0 ? (double)statistics.CompensatedSagas / statistics.TotalSagas * 100 : 0,
                BusinessProcessBreakdown = statistics.BusinessProcessBreakdown
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
} 