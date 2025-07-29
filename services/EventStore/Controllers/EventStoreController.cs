using EventStore.Models;
using EventStore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventStore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventStoreController : ControllerBase
{
    private readonly IEventStoreService _eventStoreService;
    private readonly ILogger<EventStoreController> _logger;

    public EventStoreController(IEventStoreService eventStoreService, ILogger<EventStoreController> logger)
    {
        _eventStoreService = eventStoreService;
        _logger = logger;
    }

    [HttpGet("events/{aggregateType}/{aggregateId}")]
    public async Task<ActionResult<List<StoredEvent>>> GetEventsByAggregate(string aggregateType, string aggregateId)
    {
        try
        {
            var events = await _eventStoreService.GetEventsByAggregateIdAsync(aggregateId, aggregateType);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events for aggregate {AggregateType}:{AggregateId}", aggregateType, aggregateId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("events/type/{eventType}")]
    public async Task<ActionResult<List<StoredEvent>>> GetEventsByType(string eventType)
    {
        try
        {
            var events = await _eventStoreService.GetEventsByTypeAsync(eventType);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events by type {EventType}", eventType);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("events/date-range")]
    public async Task<ActionResult<List<StoredEvent>>> GetEventsByDateRange(
        [FromQuery] DateTime from, 
        [FromQuery] DateTime to)
    {
        try
        {
            var events = await _eventStoreService.GetEventsByDateRangeAsync(from, to);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events by date range {From} to {To}", from, to);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("replay/{aggregateType}/{aggregateId}")]
    public async Task<ActionResult<object>> ReplayEvents(string aggregateType, string aggregateId, [FromQuery] DateTime? fromDate = null)
    {
        try
        {
            var events = await _eventStoreService.GetEventsForReplayAsync(aggregateId, aggregateType, fromDate);
            
            // Reconstruct the current state by applying events
            var reconstructedState = ReconstructState(events, aggregateType);
            
            return Ok(new
            {
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                EventCount = events.Count,
                ReconstructedState = reconstructedState,
                Events = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replay events for aggregate {AggregateType}:{AggregateId}", aggregateType, aggregateId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        try
        {
            var totalEvents = await _eventStoreService.GetEventCountAsync();
            
            return Ok(new
            {
                TotalEvents = totalEvents,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event store stats");
            return StatusCode(500, "Internal server error");
        }
    }

    private object ReconstructState(List<StoredEvent> events, string aggregateType)
    {
        // This is a simplified state reconstruction
        // In a real implementation, you would have proper event handlers for each event type
        
        var state = new Dictionary<string, object>();
        
        foreach (var storedEvent in events)
        {
            try
            {
                var eventData = JsonSerializer.Deserialize<JsonElement>(storedEvent.Data);
                
                switch (storedEvent.EventType)
                {
                    case "CartCreated":
                        state["cartId"] = eventData.GetProperty("cartId").GetString();
                        state["customerId"] = eventData.GetProperty("customerId").GetString();
                        state["status"] = "Created";
                        break;
                        
                    case "ItemAddedToCart":
                        if (!state.ContainsKey("items"))
                            state["items"] = new List<object>();
                        
                        var items = (List<object>)state["items"];
                        items.Add(new
                        {
                            productId = eventData.GetProperty("productId").GetString(),
                            quantity = eventData.GetProperty("quantity").GetInt32(),
                            unitPrice = eventData.GetProperty("unitPrice").GetDecimal()
                        });
                        break;
                        
                    case "CartCheckedOut":
                        state["status"] = "CheckedOut";
                        state["totalAmount"] = eventData.GetProperty("totalAmount").GetDecimal();
                        break;
                        
                    case "OrderCreated":
                        state["orderId"] = eventData.GetProperty("orderId").GetString();
                        state["status"] = "Created";
                        state["totalAmount"] = eventData.GetProperty("totalAmount").GetDecimal();
                        break;
                        
                    case "OrderConfirmed":
                        state["status"] = "Confirmed";
                        state["paymentId"] = eventData.GetProperty("paymentId").GetString();
                        break;
                        
                    case "OrderShipped":
                        state["status"] = "Shipped";
                        state["trackingNumber"] = eventData.GetProperty("trackingNumber").GetString();
                        state["carrier"] = eventData.GetProperty("carrier").GetString();
                        break;
                        
                    case "OrderDelivered":
                        state["status"] = "Delivered";
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process event {EventType} for state reconstruction", storedEvent.EventType);
            }
        }
        
        return state;
    }
} 