using CornerShop.Services;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace CornerShop.Controllers.Api
{
    /// <summary>
    /// API controller for saga metrics and monitoring
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SagaMetricsApiController : ControllerBase
    {
        private readonly ISagaMetricsService _metricsService;
        private readonly ISagaStateManager _stateManager;
        private readonly ILogger<SagaMetricsApiController> _logger;

        public SagaMetricsApiController(
            ISagaMetricsService metricsService,
            ISagaStateManager stateManager,
            ILogger<SagaMetricsApiController> logger)
        {
            _metricsService = metricsService;
            _stateManager = stateManager;
            _logger = logger;
        }

        /// <summary>
        /// Get metrics summary
        /// </summary>
        [HttpGet("summary")]
        public ActionResult<Dictionary<string, object>> GetMetricsSummary()
        {
            try
            {
                var summary = _metricsService.GetMetricsSummary();
                return Ok(new
                {
                    Success = true,
                    Summary = summary,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metrics summary");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Get Prometheus metrics in text format
        /// </summary>
        [HttpGet("prometheus")]
        public ActionResult GetPrometheusMetrics()
        {
            try
            {
                // For now, return a simple metrics format
                var metrics = new List<string>
                {
                    "# HELP saga_total Total number of sagas",
                    "# TYPE saga_total counter",
                    "saga_total{saga_type=\"SaleSaga\"} 0",
                    "",
                    "# HELP saga_success_total Total number of successful sagas",
                    "# TYPE saga_success_total counter",
                    "saga_success_total{saga_type=\"SaleSaga\"} 0"
                };

                return Content(string.Join("\n", metrics), "text/plain; version=0.0.4; charset=utf-8");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Prometheus metrics");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Get saga performance statistics
        /// </summary>
        [HttpGet("performance")]
        public async Task<ActionResult> GetSagaPerformance()
        {
            try
            {
                var allSagas = await _stateManager.GetAllSagasAsync();
                var completedSagas = allSagas.Where(s => s.IsCompleted).ToList();
                var failedSagas = allSagas.Where(s => s.IsFailed).ToList();

                var performance = new
                {
                    TotalSagas = allSagas.Count,
                    CompletedSagas = completedSagas.Count,
                    FailedSagas = failedSagas.Count,
                    SuccessRate = allSagas.Count > 0 ? (double)completedSagas.Count / allSagas.Count : 0,
                    FailureRate = allSagas.Count > 0 ? (double)failedSagas.Count / allSagas.Count : 0,
                    AverageTransitions = allSagas.Count > 0 ? allSagas.Average(s => s.Transitions.Count) : 0,
                    RecentSagas = allSagas.Where(s => s.CreatedAt > DateTime.UtcNow.AddHours(-1)).Count(),
                    RecentCompletions = completedSagas.Where(s => s.CompletedAt > DateTime.UtcNow.AddHours(-1)).Count(),
                    RecentFailures = failedSagas.Where(s => s.CreatedAt > DateTime.UtcNow.AddHours(-1)).Count()
                };

                return Ok(new
                {
                    Success = true,
                    Performance = performance,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get saga performance");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Get saga state distribution
        /// </summary>
        [HttpGet("state-distribution")]
        public async Task<ActionResult> GetSagaStateDistribution()
        {
            try
            {
                var allSagas = await _stateManager.GetAllSagasAsync();
                var stateDistribution = allSagas
                    .GroupBy(s => s.CurrentState)
                    .Select(g => new
                    {
                        State = g.Key.ToString(),
                        Count = g.Count(),
                        Percentage = (double)g.Count() / allSagas.Count * 100
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    StateDistribution = stateDistribution,
                    TotalSagas = allSagas.Count,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get saga state distribution");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Get saga transition analysis
        /// </summary>
        [HttpGet("transition-analysis")]
        public async Task<ActionResult> GetSagaTransitionAnalysis()
        {
            try
            {
                var allSagas = await _stateManager.GetAllSagasAsync();
                var transitions = allSagas
                    .SelectMany(s => s.Transitions)
                    .GroupBy(t => new { FromState = t.FromState.ToString(), ToState = t.ToState.ToString() })
                    .Select(g => new
                    {
                        g.Key.FromState,
                        g.Key.ToState,
                        Count = g.Count(),
                        AverageDuration = g.Any() ? g.Average(t => (DateTime.UtcNow - t.Timestamp).TotalSeconds) : 0
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    Transitions = transitions,
                    TotalTransitions = transitions.Sum(t => t.Count),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get saga transition analysis");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Get saga duration statistics
        /// </summary>
        [HttpGet("duration-stats")]
        public async Task<ActionResult> GetSagaDurationStats()
        {
            try
            {
                var allSagas = await _stateManager.GetAllSagasAsync();
                var completedSagas = allSagas.Where(s => s.IsCompleted && s.CompletedAt.HasValue).ToList();

                var durationStats = completedSagas.Any() ? new
                {
                    AverageDuration = completedSagas.Average(s => (s.CompletedAt!.Value - s.CreatedAt).TotalSeconds),
                    MinDuration = completedSagas.Min(s => (s.CompletedAt!.Value - s.CreatedAt).TotalSeconds),
                    MaxDuration = completedSagas.Max(s => (s.CompletedAt!.Value - s.CreatedAt).TotalSeconds),
                    MedianDuration = completedSagas
                        .OrderBy(s => (s.CompletedAt!.Value - s.CreatedAt).TotalSeconds)
                        .Skip(completedSagas.Count / 2)
                        .FirstOrDefault() != null
                        ? (completedSagas
                            .OrderBy(s => (s.CompletedAt!.Value - s.CreatedAt).TotalSeconds)
                            .Skip(completedSagas.Count / 2)
                            .First().CompletedAt!.Value - completedSagas
                            .OrderBy(s => (s.CompletedAt!.Value - s.CreatedAt).TotalSeconds)
                            .Skip(completedSagas.Count / 2)
                            .First().CreatedAt).TotalSeconds : 0,
                    TotalCompleted = completedSagas.Count
                } : new
                {
                    AverageDuration = 0.0,
                    MinDuration = 0.0,
                    MaxDuration = 0.0,
                    MedianDuration = 0.0,
                    TotalCompleted = 0
                };

                return Ok(new
                {
                    Success = true,
                    DurationStats = durationStats,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get saga duration stats");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Get recent saga activity
        /// </summary>
        [HttpGet("recent-activity")]
        public async Task<ActionResult> GetRecentSagaActivity([FromQuery] int hours = 1)
        {
            try
            {
                var allSagas = await _stateManager.GetAllSagasAsync();
                var cutoffTime = DateTime.UtcNow.AddHours(-hours);

                var recentSagas = allSagas
                    .Where(s => s.CreatedAt > cutoffTime)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(50)
                    .Select(s => new
                    {
                        s.SagaId,
                        s.SagaType,
                        s.CurrentState,
                        s.CreatedAt,
                        s.CompletedAt,
                        Duration = s.CompletedAt.HasValue ? (s.CompletedAt.Value - s.CreatedAt).TotalSeconds : 0,
                        TransitionCount = s.Transitions.Count,
                        LastTransition = s.GetLastTransition()?.Timestamp
                    })
                    .ToList();

                return Ok(new
                {
                    Success = true,
                    RecentSagas = recentSagas,
                    TimeWindow = $"{hours} hour(s)",
                    TotalInWindow = allSagas.Count(s => s.CreatedAt > cutoffTime),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent saga activity");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Get metrics for Grafana integration
        /// </summary>
        [HttpGet("grafana")]
        public ActionResult GetGrafanaMetrics()
        {
            try
            {
                var metrics = new
                {
                    Metrics = new Dictionary<string, string>
                    {
                        ["SagaTotal"] = "saga_total",
                        ["SagaSuccess"] = "saga_success_total",
                        ["SagaFailure"] = "saga_failure_total",
                        ["SagaDuration"] = "saga_duration_seconds",
                        ["SagaStepTotal"] = "saga_step_total",
                        ["SagaStepSuccess"] = "saga_step_success_total",
                        ["SagaStepFailure"] = "saga_step_failure_total",
                        ["SagaStepDuration"] = "saga_step_duration_seconds",
                        ["StateTransition"] = "saga_state_transition_total",
                        ["CompensationTotal"] = "saga_compensation_total",
                        ["CompensationSuccess"] = "saga_compensation_success_total",
                        ["CompensationFailure"] = "saga_compensation_failure_total",
                        ["CompensationDuration"] = "saga_compensation_duration_seconds",
                        ["ControlledFailure"] = "controlled_failure_total",
                        ["BusinessEvent"] = "business_event_total",
                        ["ActiveSagas"] = "saga_active",
                        ["SagasByState"] = "saga_by_state"
                    },
                    PrometheusEndpoint = "/api/SagaMetrics/prometheus",
                    DashboardUrl = "/grafana/d/saga-monitoring/saga-orchestration-monitoring"
                };

                return Ok(new
                {
                    Success = true,
                    GrafanaIntegration = metrics,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Grafana metrics");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }
    }
}
