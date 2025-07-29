using Microsoft.AspNetCore.Mvc;

namespace CartService.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<object> Get()
    {
        var instanceId = Environment.GetEnvironmentVariable("SERVICE_INSTANCE") ?? "unknown";
        var hostname = Environment.MachineName;
        var timestamp = DateTime.UtcNow;

        var healthInfo = new
        {
            status = "healthy",
            service = "cart-service",
            instance = instanceId,
            hostname,
            timestamp,
            version = "1.0.0",
            uptime = Environment.TickCount64
        };

        _logger.LogInformation("Health check requested for instance {InstanceId}", instanceId);

        return Ok(healthInfo);
    }
}
