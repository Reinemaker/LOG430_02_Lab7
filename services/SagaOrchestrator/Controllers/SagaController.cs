using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace SagaOrchestrator.Controllers
{
    /// <summary>
    /// Saga Orchestrator API Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SagaController : ControllerBase
    {
        private readonly ISagaOrchestrator _sagaOrchestrator;
        private readonly IEventProducer _eventProducer;
        private readonly ILogger<SagaController> _logger;

        public SagaController(
            ISagaOrchestrator sagaOrchestrator,
            IEventProducer eventProducer,
            ILogger<SagaController> logger)
        {
            _sagaOrchestrator = sagaOrchestrator;
            _eventProducer = eventProducer;
            _logger = logger;
        }

        /// <summary>
        /// Execute a saga orchestration
        /// </summary>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(SagaOrchestrationResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> ExecuteSaga([FromBody] SagaOrchestrationRequest request)
        {
            try
            {
                _logger.LogInformation("Received saga execution request: {SagaId} | {SagaType} | {OrderId}", 
                    request.SagaId, request.SagaType, request.OrderId);

                var response = await _sagaOrchestrator.ExecuteSagaAsync(request);

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    status = 200,
                    data = response,
                    links = new
                    {
                        self = Url.Action(nameof(ExecuteSaga), "Saga", null, Request.Scheme),
                        status = Url.Action(nameof(GetSagaStatus), "Saga", new { sagaId = response.SagaId }, Request.Scheme),
                        metrics = Url.Action(nameof(GetSagaMetrics), "Saga", null, Request.Scheme)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga execution failed: {SagaId}", request.SagaId);
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
        /// Get saga status
        /// </summary>
        [HttpGet("status/{sagaId}")]
        [ProducesResponseType(typeof(SagaOrchestrationResponse), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetSagaStatus(string sagaId)
        {
            try
            {
                var response = await _sagaOrchestrator.GetSagaStatusAsync(sagaId);

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    status = 200,
                    data = response,
                    links = new
                    {
                        self = Url.Action(nameof(GetSagaStatus), "Saga", new { sagaId }, Request.Scheme),
                        compensate = Url.Action(nameof(CompensateSaga), "Saga", new { sagaId }, Request.Scheme),
                        metrics = Url.Action(nameof(GetSagaMetrics), "Saga", null, Request.Scheme)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get saga status: {SagaId}", sagaId);
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
        /// Compensate a saga
        /// </summary>
        [HttpPost("compensate/{sagaId}")]
        [ProducesResponseType(typeof(SagaOrchestrationResponse), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> CompensateSaga(string sagaId, [FromBody] CompensateSagaRequest request)
        {
            try
            {
                _logger.LogInformation("Received saga compensation request: {SagaId} | Reason: {Reason}", 
                    sagaId, request.Reason);

                var response = await _sagaOrchestrator.CompensateSagaAsync(sagaId, request.Reason);

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    status = 200,
                    data = response,
                    links = new
                    {
                        self = Url.Action(nameof(CompensateSaga), "Saga", new { sagaId }, Request.Scheme),
                        status = Url.Action(nameof(GetSagaStatus), "Saga", new { sagaId }, Request.Scheme),
                        metrics = Url.Action(nameof(GetSagaMetrics), "Saga", null, Request.Scheme)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga compensation failed: {SagaId}", sagaId);
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
        /// Get saga metrics
        /// </summary>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(SagaMetrics), 200)]
        public async Task<IActionResult> GetSagaMetrics()
        {
            try
            {
                var metrics = await _sagaOrchestrator.GetSagaMetricsAsync();

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    status = 200,
                    data = metrics,
                    links = new
                    {
                        self = Url.Action(nameof(GetSagaMetrics), "Saga", null, Request.Scheme),
                        event_stats = Url.Action(nameof(GetEventStatistics), "Saga", null, Request.Scheme)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get saga metrics");
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
        /// Get event statistics
        /// </summary>
        [HttpGet("events/statistics")]
        [ProducesResponseType(typeof(EventStatistics), 200)]
        public async Task<IActionResult> GetEventStatistics()
        {
            try
            {
                var statistics = await _eventProducer.GetEventStatisticsAsync();

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    status = 200,
                    data = statistics,
                    links = new
                    {
                        self = Url.Action(nameof(GetEventStatistics), "Saga", null, Request.Scheme),
                        metrics = Url.Action(nameof(GetSagaMetrics), "Saga", null, Request.Scheme)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get event statistics");
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
        /// Get connection status
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var isConnected = await _eventProducer.IsConnectedAsync();

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    status = 200,
                    data = new
                    {
                        service = "SagaOrchestrator",
                        status = "Healthy",
                        redis_connected = isConnected,
                        uptime = Environment.TickCount64
                    },
                    links = new
                    {
                        self = Url.Action(nameof(GetHealth), "Saga", null, Request.Scheme),
                        metrics = Url.Action(nameof(GetSagaMetrics), "Saga", null, Request.Scheme)
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

        /// <summary>
        /// Demo saga execution
        /// </summary>
        [HttpPost("demo")]
        [ProducesResponseType(typeof(SagaOrchestrationResponse), 200)]
        public async Task<IActionResult> DemoSaga([FromBody] DemoSagaRequest request)
        {
            try
            {
                var sagaRequest = new SagaOrchestrationRequest
                {
                    SagaId = request.SagaId ?? Guid.NewGuid().ToString(),
                    SagaType = "OrderCreation",
                    OrderId = request.OrderId ?? Guid.NewGuid().ToString(),
                    CustomerId = request.CustomerId ?? "demo-customer-001",
                    StoreId = request.StoreId ?? "demo-store-001",
                    Items = request.Items ?? new List<OrderItem>
                    {
                        new OrderItem { ProductId = "prod-001", Quantity = 2, UnitPrice = 10.99m, TotalPrice = 21.98m },
                        new OrderItem { ProductId = "prod-002", Quantity = 1, UnitPrice = 15.50m, TotalPrice = 15.50m }
                    },
                    TotalAmount = request.TotalAmount ?? 37.48m,
                    PaymentMethod = request.PaymentMethod ?? "credit_card",
                    CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString()
                };

                _logger.LogInformation("Executing demo saga: {SagaId} | {OrderId}", sagaRequest.SagaId, sagaRequest.OrderId);

                var response = await _sagaOrchestrator.ExecuteSagaAsync(sagaRequest);

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    status = 200,
                    data = new
                    {
                        message = "Demo saga executed successfully",
                        saga = response,
                        demo_info = new
                        {
                            sagaId = sagaRequest.SagaId,
                            orderId = sagaRequest.OrderId,
                            correlationId = sagaRequest.CorrelationId
                        }
                    },
                    links = new
                    {
                        self = Url.Action(nameof(DemoSaga), "Saga", null, Request.Scheme),
                        status = Url.Action(nameof(GetSagaStatus), "Saga", new { sagaId = response.SagaId }, Request.Scheme),
                        metrics = Url.Action(nameof(GetSagaMetrics), "Saga", null, Request.Scheme)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Demo saga execution failed");
                return StatusCode(500, new
                {
                    timestamp = DateTime.UtcNow,
                    status = 500,
                    error = "Demo Saga Failed",
                    message = ex.Message,
                    path = Request.Path
                });
            }
        }
    }

    /// <summary>
    /// Request models for saga controller
    /// </summary>
    public class CompensateSagaRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class DemoSagaRequest
    {
        public string? SagaId { get; set; }
        public string? OrderId { get; set; }
        public string? CustomerId { get; set; }
        public string? StoreId { get; set; }
        public List<OrderItem>? Items { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CorrelationId { get; set; }
    }
} 