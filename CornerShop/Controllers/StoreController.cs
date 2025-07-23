using Microsoft.AspNetCore.Mvc;
using CornerShop.Services;
using CornerShop.Models;

namespace CornerShop.Controllers;

public class StoreController : Controller
{
    private readonly IStoreService _storeService;

    public StoreController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    public async Task<IActionResult> Index()
    {
        var stores = await _storeService.GetAllStores();
        return View(stores);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Store store)
    {
        if (ModelState.IsValid)
        {
            await _storeService.CreateStore(store);
            return RedirectToAction(nameof(Index));
        }
        return View(store);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var store = await _storeService.GetStoreById(id);
        if (store == null)
        {
            return NotFound();
        }
        return View(store);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Store store)
    {
        if (ModelState.IsValid)
        {
            await _storeService.UpdateStore(store);
            return RedirectToAction(nameof(Index));
        }
        return View(store);
    }

    [HttpPost]
    public async Task<IActionResult> Sync(string id)
    {
        var success = await _storeService.SyncStoreData(id);
        if (success)
        {
            TempData["Message"] = "Store synchronized successfully";
        }
        else
        {
            TempData["Error"] = "Failed to synchronize store";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _storeService.DeleteStore(id);
        return RedirectToAction(nameof(Index));
    }
}
