using Microsoft.AspNetCore.Mvc;

namespace StockService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly ILogger<StockController> _logger;

    public StockController(ILogger<StockController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<string> GetStock()
    {
        return Ok("Stock Service - Placeholder Implementation");
    }

    [HttpGet("health")]
    public ActionResult<string> Health()
    {
        return Ok("Stock Service is healthy");
    }
} 