using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using CornerShop.Services;
using CornerShop.Models;
using Microsoft.AspNetCore.Authorization;

namespace CornerShop.Controllers.Api;

[ApiController]
[Route("api/v1/sales")]
[Produces("application/json")]
[EnableCors("ApiPolicy")]
[Authorize]
public class SalesApiController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly IStoreService _storeService;
    private readonly IDatabaseService _databaseService;

    public SalesApiController(ISaleService saleService, IStoreService storeService, IDatabaseService databaseService)
    {
        _saleService = saleService;
        _storeService = storeService;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get recent sales for a store with pagination and sorting
    /// </summary>
    /// <param name="storeId">The store ID</param>
    /// <param name="limit">Number of recent sales to return (default 10, ignored if page/pageSize used)</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <param name="sortBy">Sort by field (default 'Date')</param>
    /// <param name="sortOrder">Sort order: asc or desc (default 'desc')</param>
    /// <returns>List of recent sales</returns>
    [HttpGet("store/{storeId}/recent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Sale>>>> GetRecentSales(
        string storeId,
        [FromQuery] int limit = 10,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "Date",
        [FromQuery] string sortOrder = "desc")
    {
        try
        {
            Console.WriteLine($"GetRecentSales called with storeId: {storeId}, limit: {limit}, page: {page}");

            var store = await _storeService.GetStoreById(storeId);
            if (store == null)
            {
                Console.WriteLine($"GetRecentSales store not found: {storeId}");
                return NotFound(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 404,
                    Error = "Not Found",
                    Message = $"Store with ID {storeId} not found",
                    Path = Request.Path
                });
            }

            Console.WriteLine($"GetRecentSales store found: {store.Name}");
            var sales = await _saleService.GetRecentSales(storeId, Math.Max(limit, page * pageSize));
            Console.WriteLine($"GetRecentSales found {sales?.Count ?? 0} sales");

            if (sales == null)
            {
                sales = new List<Sale>();
            }
            // Sorting
            sales = sortBy.ToLower() switch
            {
                "date" => (sortOrder == "asc" ? sales.OrderBy(s => s.Date) : sales.OrderByDescending(s => s.Date)).ToList(),
                "totalamount" => (sortOrder == "asc" ? sales.OrderBy(s => s.TotalAmount) : sales.OrderByDescending(s => s.TotalAmount)).ToList(),
                _ => sales.OrderByDescending(s => s.Date).ToList()
            };

            // Pagination
            var paged = sales.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            Console.WriteLine($"GetRecentSales returning {paged.Count} sales after pagination");

            var response = new ApiResponse<IEnumerable<Sale>>
            {
                Data = paged,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetRecentSales), new { storeId, limit, page, pageSize, sortBy, sortOrder }) ?? "", Rel = "self", Method = "GET" },
                    new Link { Href = Url.Action(nameof(CreateSale)) ?? "", Rel = "create", Method = "POST" },
                    new Link { Href = Url.Action(nameof(GetSalesByDateRange), new { storeId }) ?? "", Rel = "date-range", Method = "GET" }
                }
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetRecentSales error: {ex.Message}");
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
    /// Get a specific sale by ID
    /// </summary>
    /// <param name="id">The sale ID</param>
    /// <returns>The sale with details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<Sale>>> GetSale(string id)
    {
        var sale = await _saleService.GetSaleById(id);
        if (sale == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Sale with ID {id} not found",
                Path = Request.Path
            });
        }

        var response = new ApiResponse<Sale>
        {
            Data = sale,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetSale), new { id }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetSaleDetails), new { id }) ?? "", Rel = "details", Method = "GET" },
                new Link { Href = Url.Action(nameof(CancelSale), new { id }) ?? "", Rel = "cancel", Method = "POST" }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get sale details including items
    /// </summary>
    /// <param name="id">The sale ID</param>
    /// <returns>The sale with detailed items</returns>
    [HttpGet("{id}/details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<SaleDetailsViewModel>>> GetSaleDetails(string id)
    {
        var sale = await _saleService.GetSaleById(id);
        if (sale == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Sale with ID {id} not found",
                Path = Request.Path
            });
        }

        var store = await _storeService.GetStoreById(sale.StoreId);

        var saleDetails = new SaleDetailsViewModel
        {
            Sale = sale,
            StoreName = store?.Name ?? "Unknown Store",
            StoreLocation = store?.Location ?? "Unknown Location",
            Items = sale.Items.Select(item => new SaleProductDetails
            {
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Price = item.Price,
                Total = item.Quantity * item.Price
            }).ToList(),
            Subtotal = sale.TotalAmount
        };

        var response = new ApiResponse<SaleDetailsViewModel>
        {
            Data = saleDetails,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetSaleDetails), new { id }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetSale), new { id }) ?? "", Rel = "sale", Method = "GET" },
                new Link { Href = Url.Action(nameof(CancelSale), new { id }) ?? "", Rel = "cancel", Method = "POST" }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new sale
    /// </summary>
    /// <param name="saleRequest">The sale data including items</param>
    /// <returns>The created sale</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Sale>>> CreateSale([FromBody] CreateSaleRequest saleRequest)
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

        var store = await _storeService.GetStoreById(saleRequest.StoreId);
        if (store == null)
        {
            return BadRequest(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 400,
                Error = "Bad Request",
                Message = $"Store with ID {saleRequest.StoreId} not found",
                Path = Request.Path
            });
        }

        // Validate sale items
        var saleItems = saleRequest.Items.Select(item => new SaleItem
        {
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            Price = item.UnitPrice
        }).ToList();

        if (!await _saleService.ValidateSaleItems(saleItems, saleRequest.StoreId))
        {
            return BadRequest(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 400,
                Error = "Bad Request",
                Message = "One or more sale items are invalid",
                Path = Request.Path
            });
        }

        var sale = new Sale
        {
            StoreId = saleRequest.StoreId,
            Date = DateTime.UtcNow,
            Items = saleItems,
            TotalAmount = await _saleService.CalculateSaleTotal(saleItems, saleRequest.StoreId),
            Status = "Completed"
        };

        var saleId = await _saleService.CreateSale(sale);
        sale.Id = saleId;

        var response = new ApiResponse<Sale>
        {
            Data = sale,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetSale), new { id = sale.Id }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetSaleDetails), new { id = sale.Id }) ?? "", Rel = "details", Method = "GET" },
                new Link { Href = Url.Action(nameof(CancelSale), new { id = sale.Id }) ?? "", Rel = "cancel", Method = "POST" }
            }
        };

        return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, response);
    }

    /// <summary>
    /// Cancel a sale
    /// </summary>
    /// <param name="id">The sale ID</param>
    /// <param name="storeId">The store ID</param>
    /// <returns>No content</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelSale(string id, [FromQuery] string storeId)
    {
        var sale = await _saleService.GetSaleById(id);
        if (sale == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Sale with ID {id} not found",
                Path = Request.Path
            });
        }

        if (sale.StoreId != storeId)
        {
            return BadRequest(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 400,
                Error = "Bad Request",
                Message = "Sale does not belong to the specified store",
                Path = Request.Path
            });
        }

        var success = await _saleService.CancelSale(id, storeId);
        if (!success)
        {
            return BadRequest(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 400,
                Error = "Bad Request",
                Message = "Failed to cancel sale",
                Path = Request.Path
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Get sales by date range with pagination and sorting
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="storeId">Optional store ID to filter by</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <param name="sortBy">Sort by field (default 'Date')</param>
    /// <param name="sortOrder">Sort order: asc or desc (default 'desc')</param>
    /// <returns>List of sales in the date range</returns>
    [HttpGet("date-range")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Sale>>>> GetSalesByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? storeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "Date",
        [FromQuery] string sortOrder = "desc")
    {
        // Get recent sales for all stores or specific store
        var allSales = new List<Sale>();
        if (!string.IsNullOrEmpty(storeId))
        {
            var sales = await _saleService.GetRecentSales(storeId, 1000); // Get a large number to filter
            allSales.AddRange(sales);
        }
        else
        {
            var stores = await _storeService.GetAllStores();
            foreach (var store in stores)
            {
                var sales = await _saleService.GetRecentSales(store.Id, 1000);
                allSales.AddRange(sales);
            }
        }
        // Filter by date range
        var filteredSales = allSales.Where(s => s.Date >= startDate && s.Date <= endDate).ToList();
        // Sorting
        filteredSales = sortBy.ToLower() switch
        {
            "date" => (sortOrder == "asc" ? filteredSales.OrderBy(s => s.Date) : filteredSales.OrderByDescending(s => s.Date)).ToList(),
            "totalamount" => (sortOrder == "asc" ? filteredSales.OrderBy(s => s.TotalAmount) : filteredSales.OrderByDescending(s => s.TotalAmount)).ToList(),
            _ => filteredSales.OrderByDescending(s => s.Date).ToList()
        };
        // Pagination
        var paged = filteredSales.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var response = new ApiResponse<IEnumerable<Sale>>
        {
            Data = paged,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetSalesByDateRange), new { startDate, endDate, storeId, page, pageSize, sortBy, sortOrder }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(CreateSale)) ?? "", Rel = "create", Method = "POST" }
            }
        };
        return Ok(response);
    }

    /// <summary>
    /// Partially update a sale
    /// </summary>
    /// <param name="id">The sale ID</param>
    /// <param name="patchData">The partial update data</param>
    /// <returns>No content</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchSale(string id, [FromBody] SalePatchRequest patchData)
    {
        var sale = await _saleService.GetSaleById(id);
        if (sale == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Sale with ID {id} not found",
                Path = Request.Path
            });
        }

        // Apply partial updates
        if (patchData.Status != null) sale.Status = patchData.Status;
        if (patchData.TotalAmount.HasValue) sale.TotalAmount = patchData.TotalAmount.Value;

        // Update sale in database
        await _saleService.UpdateSale(sale);
        return NoContent();
    }
}

public class SalePatchRequest
{
    public string? Status { get; set; }
    public decimal? TotalAmount { get; set; }
}
