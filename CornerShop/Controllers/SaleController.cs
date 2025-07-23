using Microsoft.AspNetCore.Mvc;
using CornerShop.Services;
using CornerShop.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace CornerShop.Controllers
{
    public class SaleController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly IStoreService _storeService;
        private readonly ISaleService _saleService;

        public SaleController(IDatabaseService databaseService, IStoreService storeService, ISaleService saleService)
        {
            _databaseService = databaseService;
            _storeService = storeService;
            _saleService = saleService;
        }

        public async Task<IActionResult> All()
        {
            var sales = await _databaseService.GetAllSales();
            var stores = await _storeService.GetAllStores();
            var saleVMs = sales.Select(s => new CornerShop.Models.SaleWithStoreNameViewModel
            {
                Sale = s,
                StoreName = stores.FirstOrDefault(st => st.Id == s.StoreId)?.Name ?? "(No Store)"
            }).ToList();
            return View("All", saleVMs);
        }

        public async Task<IActionResult> Index(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                return RedirectToAction("Index", "Store");
            }
            var sales = await _databaseService.GetRecentSales(storeId, 50);
            ViewBag.StoreId = storeId;
            return View(sales);
        }

        public async Task<IActionResult> Create(string storeId)
        {
            if (string.IsNullOrEmpty(storeId))
            {
                return RedirectToAction("Index", "Store");
            }
            var products = await _databaseService.GetAllProducts(storeId);
            ViewBag.StoreId = storeId;
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string storeId, Dictionary<string, int> productQuantities)
        {
            if (string.IsNullOrEmpty(storeId) || productQuantities == null || !productQuantities.Any())
            {
                return RedirectToAction("Index", new { storeId });
            }
            var products = await _databaseService.GetAllProducts(storeId);
            var saleItems = new List<SaleItem>();
            decimal total = 0;
            foreach (var pq in productQuantities)
            {
                var product = products.FirstOrDefault(p => p.Id == pq.Key);
                if (product != null && pq.Value > 0)
                {
                    saleItems.Add(new SaleItem
                    {
                        ProductName = product.Name,
                        Quantity = pq.Value,
                        Price = product.Price
                    });
                    total += product.Price * pq.Value;
                }
            }
            if (!saleItems.Any())
            {
                return RedirectToAction("Index", new { storeId });
            }
            var sale = new Sale
            {
                StoreId = storeId,
                Items = saleItems,
                TotalAmount = total,
                Date = DateTime.UtcNow,
                Status = "Completed"
            };
            await _saleService.CreateSale(sale);
            return RedirectToAction("Index", new { storeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, string storeId)
        {
            var sale = await _databaseService.GetSaleById(id);
            if (sale == null || sale.StoreId != storeId)
            {
                return NotFound();
            }

            // Only allow cancelling if the sale is not already cancelled
            if (sale.Status != "Cancelled")
            {
                // Restore stock for each item
                foreach (var item in sale.Items)
                {
                    await _databaseService.UpdateProductStock(item.ProductName, storeId, -item.Quantity);
                }

                sale.Status = "Cancelled";
                await _databaseService.UpdateSale(sale);
            }

            return RedirectToAction("Index", new { storeId });
        }

        public async Task<IActionResult> Details(string id)
        {
            var sale = await _databaseService.GetSaleById(id);
            if (sale == null)
                return NotFound();
            var store = await _storeService.GetStoreById(sale.StoreId);
            var detailsVM = new CornerShop.Models.SaleDetailsViewModel
            {
                Sale = sale,
                StoreName = store?.Name ?? "(No Store)",
                StoreLocation = store?.Location ?? "",
                Items = sale.Items.Select(item => new CornerShop.Models.SaleProductDetails
                {
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Total = item.Quantity * item.Price
                }).ToList(),
                Subtotal = sale.Items.Sum(item => item.Quantity * item.Price)
            };
            return View("Details", detailsVM);
        }
    }
}
