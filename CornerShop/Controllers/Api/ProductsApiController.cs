using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using CornerShop.Services;
using CornerShop.Models;
using Microsoft.AspNetCore.Authorization;

namespace CornerShop.Controllers.Api;

[ApiController]
[Route("api/v1/products")]
[Produces("application/json")]
[EnableCors("ApiPolicy")]
[Authorize]
public class ProductsApiController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly IStoreService _storeService;

    public ProductsApiController(IDatabaseService databaseService, IStoreService storeService)
    {
        _databaseService = databaseService;
        _storeService = storeService;
    }

    /// <summary>
    /// Get all products across all stores with pagination, sorting, and filtering
    /// </summary>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <param name="sortBy">Sort by field (default 'Name')</param>
    /// <param name="sortOrder">Sort order: asc or desc (default 'asc')</param>
    /// <param name="searchTerm">Optional search term for filtering by name/category</param>
    /// <returns>List of all products</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Product>>>> GetAllProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "Name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? searchTerm = null)
    {
        var products = await _databaseService.GetAllProducts();
        if (products == null)
            return Ok(new ApiResponse<IEnumerable<Product>> { Data = new List<Product>(), Links = new List<Link>() });
        // Filtering
        if (!string.IsNullOrEmpty(searchTerm))
        {
            products = products.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                           p.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        // Sorting
        products = sortBy.ToLower() switch
        {
            "name" => (sortOrder == "desc" ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name)).ToList(),
            "category" => (sortOrder == "desc" ? products.OrderByDescending(p => p.Category) : products.OrderBy(p => p.Category)).ToList(),
            "price" => (sortOrder == "desc" ? products.OrderByDescending(p => p.Price) : products.OrderBy(p => p.Price)).ToList(),
            _ => products.OrderBy(p => p.Name).ToList()
        };
        // Pagination
        var paged = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var response = new ApiResponse<IEnumerable<Product>>
        {
            Data = paged,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetAllProducts), new { page, pageSize, sortBy, sortOrder, searchTerm }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(CreateProduct)) ?? "", Rel = "create", Method = "POST" },
                new Link { Href = Url.Action(nameof(GetLowStockProducts)) ?? "", Rel = "low-stock", Method = "GET" }
            }
        };
        return Ok(response);
    }

    /// <summary>
    /// Get all products for a specific store
    /// </summary>
    /// <param name="storeId">The store ID</param>
    /// <returns>List of products for the store</returns>
    [HttpGet("store/{storeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Product>>>> GetProductsByStore(string storeId)
    {
        var store = await _storeService.GetStoreById(storeId);
        if (store == null)
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Store with ID {storeId} not found",
                Path = Request.Path
            });

        var products = await _databaseService.GetAllProducts(storeId);
        if (products == null)
            return Ok(new ApiResponse<IEnumerable<Product>> { Data = new List<Product>(), Links = new List<Link>() });

        var response = new ApiResponse<IEnumerable<Product>>
        {
            Data = products,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetProductsByStore), new { storeId }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(CreateProduct)) ?? "", Rel = "create", Method = "POST" },
                new Link { Href = Url.Action(nameof(GetLowStockProducts), new { storeId }) ?? "", Rel = "low-stock", Method = "GET" }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a specific product by ID
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>The product</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<Product>>> GetProduct(string id)
    {
        try
        {
            Console.WriteLine($"GetProduct called with ID: {id}");
            var product = await _databaseService.GetProductById(id);
            Console.WriteLine($"GetProduct result: {(product == null ? "null" : "found")}");

            if (product == null)
                return NotFound(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 404,
                    Error = "Not Found",
                    Message = $"Product with ID {id} not found",
                    Path = Request.Path
                });

            var response = new ApiResponse<Product>
            {
                Data = product,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetProduct), new { id }) ?? "", Rel = "self", Method = "GET" },
                    new Link { Href = Url.Action(nameof(UpdateProduct), new { id }) ?? "", Rel = "update", Method = "PUT" },
                    new Link { Href = Url.Action(nameof(PatchProduct), new { id }) ?? "", Rel = "patch", Method = "PATCH" },
                    new Link { Href = Url.Action(nameof(DeleteProduct), new { id }) ?? "", Rel = "delete", Method = "DELETE" }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetProduct error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
    /// Search products by name or category with pagination and sorting
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <param name="sortBy">Sort by field (default 'Name')</param>
    /// <param name="sortOrder">Sort order: asc or desc (default 'asc')</param>
    /// <param name="storeId">Optional store ID to filter by</param>
    /// <returns>List of matching products</returns>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Product>>>> SearchProducts(
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "Name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? storeId = null)
    {
        try
        {
            Console.WriteLine($"SearchProducts called with searchTerm: '{searchTerm}', storeId: {storeId}");

            if (string.IsNullOrEmpty(searchTerm))
            {
                Console.WriteLine("SearchProducts: Empty search term, returning BadRequest");
                var errorMessage = "Search term is required";
                return BadRequest(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 400,
                    Error = "Bad Request",
                    Message = errorMessage,
                    Path = Request.Path
                });
            }

            var products = await _databaseService.SearchProducts(searchTerm, storeId);
            Console.WriteLine($"SearchProducts found {products?.Count ?? 0} products");

            if (products == null)
                return Ok(new ApiResponse<IEnumerable<Product>> { Data = new List<Product>(), Links = new List<Link>() });

            // Sorting
            products = sortBy.ToLower() switch
            {
                "name" => (sortOrder == "desc" ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name)).ToList(),
                "category" => (sortOrder == "desc" ? products.OrderByDescending(p => p.Category) : products.OrderBy(p => p.Category)).ToList(),
                "price" => (sortOrder == "desc" ? products.OrderByDescending(p => p.Price) : products.OrderBy(p => p.Price)).ToList(),
                _ => products.OrderBy(p => p.Name).ToList()
            };

            // Pagination
            var paged = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            Console.WriteLine($"SearchProducts returning {paged.Count} products after pagination");

            var response = new ApiResponse<IEnumerable<Product>>
            {
                Data = paged,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(SearchProducts), new { searchTerm, page, pageSize, sortBy, sortOrder, storeId }) ?? "", Rel = "self", Method = "GET" },
                    new Link { Href = Url.Action(nameof(GetAllProducts)) ?? "", Rel = "all-products", Method = "GET" }
                }
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SearchProducts error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
    /// Create a new product
    /// </summary>
    /// <param name="product">The product to create</param>
    /// <returns>The created product</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Product>>> CreateProduct([FromBody] Product product)
    {
        try
        {
            Console.WriteLine($"CreateProduct called with product: {product?.Name}");

            if (product == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 400,
                    Error = "Bad Request",
                    Message = "Product data is required",
                    Path = Request.Path
                });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                Console.WriteLine($"CreateProduct validation errors: {string.Join(", ", errors)}");
                return BadRequest(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 400,
                    Error = "Bad Request",
                    Message = string.Join("; ", errors),
                    Path = Request.Path
                });
            }

            if (!string.IsNullOrEmpty(product.StoreId))
            {
                Console.WriteLine($"CreateProduct checking store: {product.StoreId}");
                var store = await _storeService.GetStoreById(product.StoreId);
                if (store == null)
                {
                    Console.WriteLine($"CreateProduct store not found: {product.StoreId}");
                    return BadRequest(new ErrorResponse
                    {
                        Timestamp = DateTime.UtcNow,
                        Status = 400,
                        Error = "Bad Request",
                        Message = $"Store with ID {product.StoreId} not found",
                        Path = Request.Path
                    });
                }
            }

            product.Id = Guid.NewGuid().ToString();
            product.LastUpdated = DateTime.UtcNow;

            Console.WriteLine($"CreateProduct calling database service with ID: {product.Id}");
            await _databaseService.CreateProduct(product);
            Console.WriteLine($"CreateProduct successfully created product: {product.Id}");

            var response = new ApiResponse<Product>
            {
                Data = product,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetProduct), new { id = product.Id }) ?? "", Rel = "self", Method = "GET" },
                    new Link { Href = Url.Action(nameof(UpdateProduct), new { id = product.Id }) ?? "", Rel = "update", Method = "PUT" },
                    new Link { Href = Url.Action(nameof(DeleteProduct), new { id = product.Id }) ?? "", Rel = "delete", Method = "DELETE" }
                }
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateProduct error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
    /// Update an existing product
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <param name="product">The updated product data</param>
    /// <returns>No content</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] Product product)
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

        var existingProduct = await _databaseService.GetProductById(id);
        if (existingProduct == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Product with ID {id} not found",
                Path = Request.Path
            });
        }

        // Update properties
        existingProduct.Name = product.Name;
        existingProduct.Category = product.Category;
        existingProduct.Price = product.Price;
        existingProduct.StockQuantity = product.StockQuantity;
        existingProduct.MinimumStockLevel = product.MinimumStockLevel;
        existingProduct.ReorderPoint = product.ReorderPoint;
        existingProduct.LastUpdated = DateTime.UtcNow;

        await _databaseService.UpdateProduct(existingProduct);
        return NoContent();
    }

    /// <summary>
    /// Partially update a product
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <param name="patchData">The partial update data</param>
    /// <returns>No content</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchProduct(string id, [FromBody] ProductPatchRequest patchData)
    {
        var existingProduct = await _databaseService.GetProductById(id);
        if (existingProduct == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Product with ID {id} not found",
                Path = Request.Path
            });
        }

        // Apply partial updates
        if (patchData.Name != null) existingProduct.Name = patchData.Name;
        if (patchData.Category != null) existingProduct.Category = patchData.Category;
        if (patchData.Price.HasValue) existingProduct.Price = patchData.Price.Value;
        if (patchData.StockQuantity.HasValue) existingProduct.StockQuantity = patchData.StockQuantity.Value;
        if (patchData.MinimumStockLevel.HasValue) existingProduct.MinimumStockLevel = patchData.MinimumStockLevel.Value;
        if (patchData.ReorderPoint.HasValue) existingProduct.ReorderPoint = patchData.ReorderPoint.Value;

        existingProduct.LastUpdated = DateTime.UtcNow;

        await _databaseService.UpdateProduct(existingProduct);
        return NoContent();
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <param name="storeId">The store ID (optional)</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(string id, [FromQuery] string? storeId = null)
    {
        var product = await _databaseService.GetProductById(id);
        if (product == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Product with ID {id} not found",
                Path = Request.Path
            });
        }

        if (!string.IsNullOrEmpty(storeId) && product.StoreId != storeId)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Product with ID {id} not found in store {storeId}",
                Path = Request.Path
            });
        }

        await _databaseService.DeleteProduct(id, storeId ?? product.StoreId);
        return NoContent();
    }

    /// <summary>
    /// Get products with low stock (below reorder point)
    /// </summary>
    /// <param name="storeId">Optional store ID to filter by</param>
    /// <returns>List of products with low stock</returns>
    [HttpGet("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Product>>>> GetLowStockProducts([FromQuery] string? storeId = null)
    {
        var products = await _databaseService.GetAllProducts(storeId);
        if (products == null)
            return Ok(new ApiResponse<IEnumerable<Product>> { Data = new List<Product>(), Links = new List<Link>() });
        var lowStockProducts = products.Where(p => p.StockQuantity <= p.ReorderPoint).ToList();

        var response = new ApiResponse<IEnumerable<Product>>
        {
            Data = lowStockProducts,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetLowStockProducts), new { storeId }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetAllProducts)) ?? "", Rel = "all-products", Method = "GET" }
            }
        };

        return Ok(response);
    }
}

// HATEOAS and API Response Models
public class ApiResponse<T>
{
    public T Data { get; set; } = default!;
    public List<Link> Links { get; set; } = new();
}

public class Link
{
    public string Href { get; set; } = string.Empty;
    public string Rel { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public DateTime Timestamp { get; set; }
    public int Status { get; set; }
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public class ProductPatchRequest
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public int? StockQuantity { get; set; }
    public int? MinimumStockLevel { get; set; }
    public int? ReorderPoint { get; set; }
}
