using Microsoft.AspNetCore.Mvc;

namespace ReportingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ILogger<ReportsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<string> GetReports()
    {
        return Ok("Reporting Service - Placeholder Implementation");
    }

    [HttpGet("health")]
    public ActionResult<string> Health()
    {
        return Ok("Reporting Service is healthy");
    }
} 