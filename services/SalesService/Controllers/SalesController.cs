using Microsoft.AspNetCore.Mvc;

namespace SalesService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ILogger<SalesController> _logger;

    public SalesController(ILogger<SalesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<string> GetSales()
    {
        return Ok("Sales Service - Placeholder Implementation");
    }

    [HttpGet("health")]
    public ActionResult<string> Health()
    {
        return Ok("Sales Service is healthy");
    }
} 