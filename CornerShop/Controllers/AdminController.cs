using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CornerShop.Services;

namespace CornerShop.Controllers
{
    public class AdminController : Controller
    {
        private readonly SyncService _syncService;

        public AdminController(SyncService syncService)
        {
            _syncService = syncService;
        }

        // GET: /Admin/SyncSales?storeId=STORE_ID
        public async Task<IActionResult> SyncSales(string storeId)
        {
            await _syncService.SyncAllSalesToCentralAsync(storeId);
            return RedirectToAction("ConsolidatedReport", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> SyncAllStores()
        {
            // Get all stores
            var storeService = HttpContext.RequestServices.GetService(typeof(CornerShop.Services.IStoreService)) as CornerShop.Services.IStoreService;
            if (storeService != null)
            {
                var stores = await storeService.GetAllStores();
                foreach (var store in stores)
                {
                    await _syncService.SyncAllSalesToCentralAsync(store.Id);
                }
            }
            return RedirectToAction("ConsolidatedReport", "Home");
        }
    }
}
