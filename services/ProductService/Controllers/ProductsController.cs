using Microsoft.AspNetCore.Mvc;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<string> GetProducts()
    {
        return Ok("Product Service - Placeholder Implementation");
    }

    [HttpGet("{id}")]
    public ActionResult<string> GetProduct(string id)
    {
        return Ok($"Product {id} - Placeholder Implementation");
    }

    [HttpGet("health")]
    public ActionResult<string> Health()
    {
        return Ok("Product Service is healthy");
    }
}
