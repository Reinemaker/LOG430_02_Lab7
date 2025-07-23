using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using CornerShop.Services;
using CornerShop.Models;
using Microsoft.AspNetCore.Authorization;

namespace CornerShop.Controllers.Api;

[ApiController]
[Route("api/v1/saga-state")]
[Produces("application/json")]
[EnableCors("ApiPolicy")]
[Authorize]
public class SagaStateApiController : ControllerBase
{
    private readonly ISagaStateManager _stateManager;
    private readonly ISagaEventPublisher _eventPublisher;
    private readonly ILogger<SagaStateApiController> _logger;

    public SagaStateApiController(ISagaStateManager stateManager, ISagaEventPublisher eventPublisher, ILogger<SagaStateApiController> logger)
    {
        _stateManager = stateManager;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Get all sagas with their current states
    /// </summary>
    /// <returns>List of all sagas</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SagaStateMachine>>>> GetAllSagas()
    {
        try
        {
            var sagas = await _stateManager.GetAllSagasAsync();

            var response = new ApiResponse<List<SagaStateMachine>>
            {
                Data = sagas,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetAllSagas)) ?? "", Rel = "self", Method = "GET" }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all sagas");

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
    /// Get a specific saga by ID
    /// </summary>
    /// <param name="sagaId">The saga ID</param>
    /// <returns>The saga state machine</returns>
    [HttpGet("{sagaId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SagaStateMachine>>> GetSaga(string sagaId)
    {
        try
        {
            var saga = await _stateManager.GetSagaAsync(sagaId);

            if (saga == null)
            {
                return NotFound(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 404,
                    Error = "Not Found",
                    Message = $"Saga {sagaId} not found",
                    Path = Request.Path
                });
            }

            var response = new ApiResponse<SagaStateMachine>
            {
                Data = saga,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetSaga), new { sagaId }) ?? "", Rel = "self", Method = "GET" },
                    new Link { Href = Url.Action(nameof(GetSagaTransitions), new { sagaId }) ?? "", Rel = "transitions", Method = "GET" }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saga {SagaId}", sagaId);

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
    /// Get saga transitions (state history)
    /// </summary>
    /// <param name="sagaId">The saga ID</param>
    /// <returns>List of state transitions</returns>
    [HttpGet("{sagaId}/transitions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<SagaStateTransition>>>> GetSagaTransitions(string sagaId)
    {
        try
        {
            var saga = await _stateManager.GetSagaAsync(sagaId);

            if (saga == null)
            {
                return NotFound(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 404,
                    Error = "Not Found",
                    Message = $"Saga {sagaId} not found",
                    Path = Request.Path
                });
            }

            var transitions = await _stateManager.GetSagaTransitionsAsync(sagaId);

            var response = new ApiResponse<List<SagaStateTransition>>
            {
                Data = transitions,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetSagaTransitions), new { sagaId }) ?? "", Rel = "self", Method = "GET" },
                    new Link { Href = Url.Action(nameof(GetSaga), new { sagaId }) ?? "", Rel = "saga", Method = "GET" }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transitions for saga {SagaId}", sagaId);

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
    /// Get sagas by state
    /// </summary>
    /// <param name="state">The saga state to filter by</param>
    /// <returns>List of sagas in the specified state</returns>
    [HttpGet("by-state/{state}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SagaStateMachine>>>> GetSagasByState(SagaState state)
    {
        try
        {
            var sagas = await _stateManager.GetSagasByStateAsync(state);

            var response = new ApiResponse<List<SagaStateMachine>>
            {
                Data = sagas,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetSagasByState), new { state }) ?? "", Rel = "self", Method = "GET" },
                    new Link { Href = Url.Action(nameof(GetAllSagas)) ?? "", Rel = "all", Method = "GET" }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sagas by state {State}", state);

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
    /// Get all saga events
    /// </summary>
    /// <returns>List of all saga events</returns>
    [HttpGet("events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SagaStateTransition>>>> GetAllEvents()
    {
        try
        {
            var events = _eventPublisher.GetAllEvents();

            var response = new ApiResponse<List<SagaStateTransition>>
            {
                Data = events,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetAllEvents)) ?? "", Rel = "self", Method = "GET" }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all events");

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
    /// Get events for a specific saga
    /// </summary>
    /// <param name="sagaId">The saga ID</param>
    /// <returns>List of events for the saga</returns>
    [HttpGet("{sagaId}/events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SagaStateTransition>>>> GetSagaEvents(string sagaId)
    {
        try
        {
            var events = _eventPublisher.GetEventsForSaga(sagaId);

            var response = new ApiResponse<List<SagaStateTransition>>
            {
                Data = events,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetSagaEvents), new { sagaId }) ?? "", Rel = "self", Method = "GET" },
                    new Link { Href = Url.Action(nameof(GetSaga), new { sagaId }) ?? "", Rel = "saga", Method = "GET" }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events for saga {SagaId}", sagaId);

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
