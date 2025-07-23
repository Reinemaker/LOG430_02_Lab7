using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using CornerShop.Services;
using CornerShop.Models;
using Microsoft.AspNetCore.Authorization;

namespace CornerShop.Controllers.Api;

[ApiController]
[Route("api/v1/saga")]
[Produces("application/json")]
[EnableCors("ApiPolicy")]
[Authorize]
public class SagaApiController : ControllerBase
{
    private readonly ISagaOrchestrator _sagaOrchestrator;
    private readonly ILogger<SagaApiController> _logger;

    public SagaApiController(ISagaOrchestrator sagaOrchestrator, ILogger<SagaApiController> logger)
    {
        _sagaOrchestrator = sagaOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Execute a sale saga with distributed transaction management
    /// </summary>
    /// <param name="saleRequest">The sale request</param>
    /// <returns>Saga execution result</returns>
    [HttpPost("sale")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SagaResult>>> ExecuteSaleSaga([FromBody] CreateSaleRequest saleRequest)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 400,
                Error = "Bad Request",
                Message = string.Join("; ", errors),
                Path = Request.Path
            });
        }

        try
        {
            _logger.LogInformation("Executing sale saga for store {StoreId}", saleRequest.StoreId);

            var sagaResult = await _sagaOrchestrator.ExecuteSaleSagaAsync(saleRequest);

            var response = new ApiResponse<SagaResult>
            {
                Data = sagaResult,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(ExecuteSaleSaga)) ?? "", Rel = "self", Method = "POST" },
                    new Link { Href = Url.Action(nameof(CompensateSaga), new { sagaId = sagaResult.SagaId }) ?? "", Rel = "compensate", Method = "POST" }
                }
            };

            if (sagaResult.IsSuccess)
            {
                return CreatedAtAction(nameof(ExecuteSaleSaga), new { sagaId = sagaResult.SagaId }, response);
            }
            else
            {
                return StatusCode(500, new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 500,
                    Error = "Saga Execution Failed",
                    Message = sagaResult.ErrorMessage ?? "Unknown error occurred during saga execution",
                    Path = Request.Path
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sale saga for store {StoreId}", saleRequest.StoreId);

            return StatusCode(500, new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 500,
                Error = "Internal Server Error",
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    /// <summary>
    /// Execute an order saga with distributed transaction management
    /// </summary>
    /// <param name="orderRequest">The order request</param>
    /// <returns>Saga execution result</returns>
    [HttpPost("order")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SagaResult>>> ExecuteOrderSaga([FromBody] CreateOrderRequest orderRequest)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 400,
                Error = "Bad Request",
                Message = string.Join("; ", errors),
                Path = Request.Path
            });
        }

        try
        {
            _logger.LogInformation("Executing order saga for customer {CustomerId}", orderRequest.CustomerId);

            var sagaResult = await _sagaOrchestrator.ExecuteOrderSagaAsync(orderRequest);

            var response = new ApiResponse<SagaResult>
            {
                Data = sagaResult,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(ExecuteOrderSaga)) ?? "", Rel = "self", Method = "POST" },
                    new Link { Href = Url.Action(nameof(CompensateSaga), new { sagaId = sagaResult.SagaId }) ?? "", Rel = "compensate", Method = "POST" }
                }
            };

            if (sagaResult.IsSuccess)
            {
                return CreatedAtAction(nameof(ExecuteOrderSaga), new { sagaId = sagaResult.SagaId }, response);
            }
            else
            {
                return StatusCode(500, new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 500,
                    Error = "Saga Execution Failed",
                    Message = sagaResult.ErrorMessage ?? "Unknown error occurred during saga execution",
                    Path = Request.Path
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing order saga for customer {CustomerId}", orderRequest.CustomerId);

            return StatusCode(500, new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 500,
                Error = "Internal Server Error",
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    /// <summary>
    /// Execute a stock update saga with distributed transaction management
    /// </summary>
    /// <param name="stockRequest">The stock update request</param>
    /// <returns>Saga execution result</returns>
    [HttpPost("stock")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SagaResult>>> ExecuteStockUpdateSaga([FromBody] StockUpdateRequest stockRequest)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 400,
                Error = "Bad Request",
                Message = string.Join("; ", errors),
                Path = Request.Path
            });
        }

        try
        {
            _logger.LogInformation("Executing stock update saga for product {ProductName}", stockRequest.ProductName);

            var sagaResult = await _sagaOrchestrator.ExecuteStockUpdateSagaAsync(stockRequest);

            var response = new ApiResponse<SagaResult>
            {
                Data = sagaResult,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(ExecuteStockUpdateSaga)) ?? "", Rel = "self", Method = "POST" },
                    new Link { Href = Url.Action(nameof(CompensateSaga), new { sagaId = sagaResult.SagaId }) ?? "", Rel = "compensate", Method = "POST" }
                }
            };

            if (sagaResult.IsSuccess)
            {
                return CreatedAtAction(nameof(ExecuteStockUpdateSaga), new { sagaId = sagaResult.SagaId }, response);
            }
            else
            {
                return StatusCode(500, new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 500,
                    Error = "Saga Execution Failed",
                    Message = sagaResult.ErrorMessage ?? "Unknown error occurred during saga execution",
                    Path = Request.Path
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stock update saga for product {ProductName}", stockRequest.ProductName);

            return StatusCode(500, new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 500,
                Error = "Internal Server Error",
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }

    /// <summary>
    /// Compensate a failed saga by rolling back completed steps
    /// </summary>
    /// <param name="sagaId">The saga ID to compensate</param>
    /// <returns>Compensation result</returns>
    [HttpPost("compensate/{sagaId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SagaResult>>> CompensateSaga(string sagaId)
    {
        try
        {
            _logger.LogInformation("Compensating saga {SagaId}", sagaId);

            var sagaResult = await _sagaOrchestrator.CompensateSagaAsync(sagaId);

            var response = new ApiResponse<SagaResult>
            {
                Data = sagaResult,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(CompensateSaga), new { sagaId }) ?? "", Rel = "self", Method = "POST" }
                }
            };

            if (sagaResult.IsSuccess)
            {
                return Ok(response);
            }
            else
            {
                return NotFound(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 404,
                    Error = "Saga Not Found",
                    Message = sagaResult.ErrorMessage ?? "Saga not found",
                    Path = Request.Path
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating saga {SagaId}", sagaId);

            return StatusCode(500, new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 500,
                Error = "Internal Server Error",
                Message = ex.Message,
                Path = Request.Path
            });
        }
    }
}
