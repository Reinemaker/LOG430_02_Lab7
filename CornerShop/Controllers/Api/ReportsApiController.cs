using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using CornerShop.Services;
using CornerShop.Models;
using Microsoft.AspNetCore.Authorization;

namespace CornerShop.Controllers.Api;

[ApiController]
[Route("api/v1/reports")]
[Produces("application/json")]
[EnableCors("ApiPolicy")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly IStoreService _storeService;
    private readonly IDatabaseService _databaseService;

    public ReportsApiController(ISaleService saleService, IStoreService storeService, IDatabaseService databaseService)
    {
        _saleService = saleService;
        _storeService = storeService;
        _databaseService = databaseService;
    }

    /// <summary>
    /// Get consolidated sales report across all stores
    /// </summary>
    /// <param name="startDate">Start date for the report</param>
    /// <param name="endDate">End date for the report</param>
    /// <returns>Consolidated sales report</returns>
    [HttpGet("sales/consolidated")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<ConsolidatedSalesReport>>> GetConsolidatedSalesReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var stores = await _storeService.GetAllStores();
        if (stores == null || stores.Count == 0)
        {
            return Ok(new ApiResponse<ConsolidatedSalesReport>
            {
                Data = new ConsolidatedSalesReport
                {
                    StartDate = startDate ?? DateTime.UtcNow,
                    EndDate = endDate ?? DateTime.UtcNow,
                    TotalSales = 0,
                    TotalRevenue = 0,
                    AverageSaleAmount = 0,
                    StoreReports = new List<StoreSalesReport>()
                },
                Links = new List<Link>()
            });
        }
        var allSales = new List<Sale>();

        // Get sales from all stores
        foreach (var store in stores)
        {
            var sales = await _saleService.GetRecentSales(store.Id, 1000);
            allSales.AddRange(sales);
        }

        // Filter by date range if provided
        if (startDate.HasValue)
        {
            allSales = allSales.Where(s => s.Date >= startDate.Value).ToList();
        }
        if (endDate.HasValue)
        {
            allSales = allSales.Where(s => s.Date <= endDate.Value).ToList();
        }

        var report = new ConsolidatedSalesReport
        {
            StartDate = startDate ?? (allSales.Any() ? allSales.Min(s => s.Date) : DateTime.UtcNow),
            EndDate = endDate ?? (allSales.Any() ? allSales.Max(s => s.Date) : DateTime.UtcNow),
            TotalSales = allSales.Count,
            TotalRevenue = allSales.Sum(s => s.TotalAmount),
            AverageSaleAmount = allSales.Any() ? allSales.Average(s => s.TotalAmount) : 0,
            StoreReports = new List<StoreSalesReport>()
        };

        // Group by store
        foreach (var store in stores)
        {
            var storeSales = allSales.Where(s => s.StoreId == store.Id).ToList();
            var storeReport = new StoreSalesReport
            {
                StoreId = store.Id,
                StoreName = store.Name,
                TotalSales = storeSales.Count,
                TotalRevenue = storeSales.Sum(s => s.TotalAmount),
                AverageSaleAmount = storeSales.Any() ? storeSales.Average(s => s.TotalAmount) : 0
            };
            report.StoreReports.Add(storeReport);
        }

        var response = new ApiResponse<ConsolidatedSalesReport>
        {
            Data = report,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetConsolidatedSalesReport), new { startDate, endDate }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetInventoryReport)) ?? "", Rel = "inventory", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetTopSellingProducts)) ?? "", Rel = "top-selling", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetSalesTrendReport)) ?? "", Rel = "trend", Method = "GET" }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get inventory report across all stores
    /// </summary>
    /// <returns>Inventory status report</returns>
    [HttpGet("inventory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<InventoryReport>>> GetInventoryReport()
    {
        var products = await _databaseService.GetAllProducts();
        var stores = await _storeService.GetAllStores();

        var report = new InventoryReport
        {
            TotalProducts = products.Count,
            TotalStores = stores.Count,
            LowStockProducts = products.Where(p => p.StockQuantity <= p.ReorderPoint).Count(),
            OutOfStockProducts = products.Where(p => p.StockQuantity == 0).Count(),
            StoreInventory = new List<StoreInventoryReport>()
        };

        // Group by store
        foreach (var store in stores)
        {
            var storeProducts = products.Where(p => p.StoreId == store.Id).ToList();
            var storeInventory = new StoreInventoryReport
            {
                StoreId = store.Id,
                StoreName = store.Name,
                TotalProducts = storeProducts.Count,
                LowStockProducts = storeProducts.Where(p => p.StockQuantity <= p.ReorderPoint).Count(),
                OutOfStockProducts = storeProducts.Where(p => p.StockQuantity == 0).Count(),
                TotalInventoryValue = storeProducts.Sum(p => p.StockQuantity * p.Price)
            };
            report.StoreInventory.Add(storeInventory);
        }

        var response = new ApiResponse<InventoryReport>
        {
            Data = report,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetInventoryReport)) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetConsolidatedSalesReport)) ?? "", Rel = "sales", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetTopSellingProducts)) ?? "", Rel = "top-selling", Method = "GET" }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get top selling products report
    /// </summary>
    /// <param name="limit">Number of top products to return</param>
    /// <param name="storeId">Optional store ID to filter by</param>
    /// <returns>Top selling products report</returns>
    [HttpGet("products/top-selling")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TopSellingProductReport>>>> GetTopSellingProducts(
        [FromQuery] int limit = 10,
        [FromQuery] string? storeId = null)
    {
        var allSales = new List<Sale>();

        if (!string.IsNullOrEmpty(storeId))
        {
            var sales = await _saleService.GetRecentSales(storeId, 1000);
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

        // Extract all sale items
        var allSaleItems = allSales.SelectMany(sale => sale.Items).ToList();

        // Group by product and calculate totals
        var productSales = allSaleItems
            .GroupBy(item => item.ProductName)
            .Select(g => new TopSellingProductReport
            {
                ProductName = g.Key,
                TotalQuantitySold = g.Sum(item => item.Quantity),
                TotalRevenue = g.Sum(item => item.Quantity * item.Price),
                NumberOfSales = g.Count()
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .Take(limit)
            .ToList();

        var response = new ApiResponse<IEnumerable<TopSellingProductReport>>
        {
            Data = productSales,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetTopSellingProducts), new { limit, storeId }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetConsolidatedSalesReport)) ?? "", Rel = "sales", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetInventoryReport)) ?? "", Rel = "inventory", Method = "GET" }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get sales trend report
    /// </summary>
    /// <param name="period">Period grouping (daily, weekly, monthly)</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Sales trend report</returns>
    [HttpGet("sales/trend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<SalesTrendReport>>>> GetSalesTrendReport(
        [FromQuery] string period = "daily",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var stores = await _storeService.GetAllStores();
        var allSales = new List<Sale>();

        // Get sales from all stores
        foreach (var store in stores)
        {
            var sales = await _saleService.GetRecentSales(store.Id, 1000);
            allSales.AddRange(sales);
        }

        // Filter by date range if provided
        if (startDate.HasValue)
        {
            allSales = allSales.Where(s => s.Date >= startDate.Value).ToList();
        }
        if (endDate.HasValue)
        {
            allSales = allSales.Where(s => s.Date <= endDate.Value).ToList();
        }

        // Group by period
        var trendReports = new List<SalesTrendReport>();

        switch (period.ToLower())
        {
            case "daily":
                trendReports = allSales
                    .GroupBy(s => s.Date.Date)
                    .Select(g => new SalesTrendReport
                    {
                        Period = g.Key.ToString("yyyy-MM-dd"),
                        TotalSales = g.Count(),
                        TotalRevenue = g.Sum(s => s.TotalAmount),
                        AverageSaleAmount = g.Average(s => s.TotalAmount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
                break;

            case "weekly":
                trendReports = allSales
                    .GroupBy(s => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.Date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                    .Select(g => new SalesTrendReport
                    {
                        Period = $"Week {g.Key}",
                        TotalSales = g.Count(),
                        TotalRevenue = g.Sum(s => s.TotalAmount),
                        AverageSaleAmount = g.Average(s => s.TotalAmount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
                break;

            case "monthly":
                trendReports = allSales
                    .GroupBy(s => new { s.Date.Year, s.Date.Month })
                    .Select(g => new SalesTrendReport
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        TotalSales = g.Count(),
                        TotalRevenue = g.Sum(s => s.TotalAmount),
                        AverageSaleAmount = g.Average(s => s.TotalAmount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
                break;

            default:
                return BadRequest(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 400,
                    Error = "Bad Request",
                    Message = "Period must be 'daily', 'weekly', or 'monthly'",
                    Path = Request.Path
                });
        }

        var response = new ApiResponse<IEnumerable<SalesTrendReport>>
        {
            Data = trendReports,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetSalesTrendReport), new { period, startDate, endDate }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetConsolidatedSalesReport)) ?? "", Rel = "sales", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetTopSellingProducts)) ?? "", Rel = "top-selling", Method = "GET" }
            }
        };

        return Ok(response);
    }
}

// Report models
public class ConsolidatedSalesReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSaleAmount { get; set; }
    public List<StoreSalesReport> StoreReports { get; set; } = new();
}

public class StoreSalesReport
{
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSaleAmount { get; set; }
}

public class InventoryReport
{
    public int TotalProducts { get; set; }
    public int TotalStores { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public List<StoreInventoryReport> StoreInventory { get; set; } = new();
}

public class StoreInventoryReport
{
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
}

public class TopSellingProductReport
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int NumberOfSales { get; set; }
}

public class SalesTrendReport
{
    public string Period { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSaleAmount { get; set; }
}
