using CornerShop.Services;
using CornerShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace CornerShop.Controllers.Api
{
    /// <summary>
    /// API controller for managing controlled failures in saga orchestration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ControlledFailureApiController : ControllerBase
    {
        private readonly IControlledFailureService _failureService;
        private readonly ISagaStateManager _stateManager;
        private readonly ILogger<ControlledFailureApiController> _logger;

        public ControlledFailureApiController(
            IControlledFailureService failureService,
            ISagaStateManager stateManager,
            ILogger<ControlledFailureApiController> logger)
        {
            _failureService = failureService;
            _stateManager = stateManager;
            _logger = logger;
        }

        /// <summary>
        /// Get current failure configuration
        /// </summary>
        [HttpGet("config")]
        public ActionResult<Dictionary<string, object>> GetFailureConfiguration()
        {
            try
            {
                var config = _failureService.GetFailureConfiguration();
                return Ok(new
                {
                    Success = true,
                    Configuration = config,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get failure configuration");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Update failure configuration
        /// </summary>
        [HttpPut("config")]
        public ActionResult UpdateFailureConfiguration([FromBody] Dictionary<string, object> config)
        {
            try
            {
                _failureService.UpdateFailureConfiguration(config);
                return Ok(new
                {
                    Success = true,
                    Message = "Failure configuration updated successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update failure configuration");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Enable or disable controlled failures
        /// </summary>
        [HttpPost("toggle")]
        public ActionResult ToggleFailures([FromBody] bool enable)
        {
            try
            {
                var config = new Dictionary<string, object>
                {
                    ["EnableFailures"] = enable
                };
                _failureService.UpdateFailureConfiguration(config);

                return Ok(new
                {
                    Success = true,
                    Message = $"Controlled failures {(enable ? "enabled" : "disabled")}",
                    EnableFailures = enable,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle failures");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Set failure probability for a specific failure type
        /// </summary>
        [HttpPost("probability")]
        public ActionResult SetFailureProbability([FromBody] SetFailureProbabilityRequest request)
        {
            try
            {
                var config = new Dictionary<string, object>
                {
                    [request.FailureType] = request.Probability
                };
                _failureService.UpdateFailureConfiguration(config);

                return Ok(new
                {
                    Success = true,
                    Message = $"Failure probability for {request.FailureType} set to {request.Probability}",
                    request.FailureType,
                    request.Probability,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set failure probability");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Get saga states affected by failures
        /// </summary>
        [HttpGet("affected-sagas")]
        public async Task<ActionResult> GetAffectedSagas()
        {
            try
            {
                var allSagas = await _stateManager.GetAllSagasAsync();
                var affectedSagas = allSagas.Where(s => s.IsFailed || s.IsCompensating || s.HasTransition(SagaState.Compensated)).ToList();

                return Ok(new
                {
                    Success = true,
                    AffectedSagas = affectedSagas.Select(s => new
                    {
                        s.SagaId,
                        s.SagaType,
                        s.CurrentState,
                        s.ErrorMessage,
                        s.CreatedAt,
                        s.CompletedAt,
                        TransitionCount = s.Transitions.Count,
                        LastTransition = s.GetLastTransition()
                    }),
                    TotalAffected = affectedSagas.Count,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get affected sagas");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Get compensation statistics
        /// </summary>
        [HttpGet("compensation-stats")]
        public async Task<ActionResult> GetCompensationStats()
        {
            try
            {
                var allSagas = await _stateManager.GetAllSagasAsync();
                var compensatedSagas = allSagas.Where(s => s.HasTransition(SagaState.Compensated)).ToList();
                var failedSagas = allSagas.Where(s => s.IsFailed).ToList();

                var stats = new
                {
                    TotalSagas = allSagas.Count,
                    CompensatedSagas = compensatedSagas.Count,
                    FailedSagas = failedSagas.Count,
                    CompensationRate = allSagas.Count > 0 ? (double)compensatedSagas.Count / allSagas.Count : 0,
                    FailureRate = allSagas.Count > 0 ? (double)failedSagas.Count / allSagas.Count : 0,
                    RecentFailures = failedSagas.Where(s => s.CreatedAt > DateTime.UtcNow.AddHours(-1)).Count(),
                    RecentCompensations = compensatedSagas.Where(s => s.CompletedAt > DateTime.UtcNow.AddHours(-1)).Count()
                };

                return Ok(new
                {
                    Success = true,
                    Statistics = stats,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get compensation stats");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Simulate a specific failure type
        /// </summary>
        [HttpPost("simulate")]
        public async Task<ActionResult> SimulateFailure([FromBody] SimulateFailureRequest request)
        {
            try
            {
                bool failureTriggered = false;
                string result = "No failure triggered";

                switch (request.FailureType.ToLower())
                {
                    case "insufficientstock":
                        try
                        {
                            await _failureService.SimulateInsufficientStockAsync(request.ProductName!, request.StoreId!, request.Quantity!.Value);
                        }
                        catch (Exception ex)
                        {
                            failureTriggered = true;
                            result = ex.Message;
                        }
                        break;

                    case "paymentfailure":
                        try
                        {
                            await _failureService.SimulatePaymentFailureAsync(request.Amount!.Value, request.CustomerId!);
                        }
                        catch (Exception ex)
                        {
                            failureTriggered = true;
                            result = ex.Message;
                        }
                        break;

                    case "networktimeout":
                        try
                        {
                            await _failureService.SimulateNetworkTimeoutAsync(request.ServiceName!);
                        }
                        catch (Exception ex)
                        {
                            failureTriggered = true;
                            result = ex.Message;
                        }
                        break;

                    case "databasefailure":
                        try
                        {
                            await _failureService.SimulateDatabaseFailureAsync(request.Operation!);
                        }
                        catch (Exception ex)
                        {
                            failureTriggered = true;
                            result = ex.Message;
                        }
                        break;

                    case "serviceunavailable":
                        try
                        {
                            await _failureService.SimulateServiceUnavailableAsync(request.ServiceName!);
                        }
                        catch (Exception ex)
                        {
                            failureTriggered = true;
                            result = ex.Message;
                        }
                        break;

                    default:
                        return BadRequest(new { Success = false, Error = $"Unknown failure type: {request.FailureType}" });
                }

                return Ok(new
                {
                    Success = true,
                    FailureTriggered = failureTriggered,
                    Result = result,
                    request.FailureType,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to simulate failure");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }
    }

    public class SetFailureProbabilityRequest
    {
        public string FailureType { get; set; } = string.Empty;
        public double Probability { get; set; }
    }

    public class SimulateFailureRequest
    {
        public string FailureType { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? StoreId { get; set; }
        public int? Quantity { get; set; }
        public decimal? Amount { get; set; }
        public string? CustomerId { get; set; }
        public string? ServiceName { get; set; }
        public string? Operation { get; set; }
    }
}
