using Microsoft.AspNetCore.Mvc;
using CornerShop.Services;
using CornerShop.Models;

namespace CornerShop.Controllers;

public class HomeController : Controller
{
    private readonly IStoreService _storeService;
    private readonly IDatabaseService _databaseService;

    public HomeController(IStoreService storeService, IDatabaseService databaseService)
    {
        _storeService = storeService;
        _databaseService = databaseService;
    }

    public async Task<IActionResult> Index()
    {
        var stores = await _storeService.GetAllStores();
        return View(stores);
    }

    public async Task<IActionResult> Dashboard(string storeId)
    {
        if (string.IsNullOrEmpty(storeId))
        {
            return RedirectToAction("Index");
        }

        var store = await _storeService.GetStoreById(storeId);
        if (store == null)
        {
            return NotFound();
        }

        var stats = await _storeService.GetStoreStatistics(storeId);
        ViewBag.Store = store;
        return View(stats);
    }

    public async Task<IActionResult> ConsolidatedReport(DateTime? startDate, DateTime? endDate)
    {
        startDate ??= DateTime.Today.AddDays(-30);
        endDate ??= DateTime.Today;

        var report = await _databaseService.GetConsolidatedReport(startDate.Value, endDate.Value);
        return View(report);
    }

    public IActionResult ApiDocumentation()
    {
        return View();
    }
}
