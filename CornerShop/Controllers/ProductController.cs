using Microsoft.AspNetCore.Mvc;
using CornerShop.Services;
using CornerShop.Models;

namespace CornerShop.Controllers;

public class ProductController : Controller
{
    private readonly IDatabaseService _databaseService;
    private readonly IStoreService _storeService;

    public ProductController(IDatabaseService databaseService, IStoreService storeService)
    {
        _databaseService = databaseService;
        _storeService = storeService;
    }

    public async Task<IActionResult> Index(string storeId)
    {
        if (string.IsNullOrEmpty(storeId))
        {
            return RedirectToAction("Index", "Store");
        }

        var store = await _storeService.GetStoreById(storeId);
        if (store == null)
        {
            return NotFound();
        }

        var products = await _databaseService.GetAllProducts(storeId);
        ViewBag.Store = store;
        return View(products);
    }

    public async Task<IActionResult> Create(string storeId)
    {
        if (string.IsNullOrEmpty(storeId))
        {
            return RedirectToAction("Index", "Store");
        }

        var store = await _storeService.GetStoreById(storeId);
        if (store == null)
        {
            return NotFound();
        }

        // Get all products that are not already in this store
        var existingProducts = await _databaseService.GetAllProducts(storeId);
        var allProducts = await _databaseService.GetAllProducts();
        var availableProducts = allProducts.Where(p => !existingProducts.Any(ep => ep.Name == p.Name)).ToList();

        ViewBag.Store = store;
        ViewBag.StoreId = storeId;
        return View(availableProducts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToStore(string productId, string storeId)
    {
        if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(storeId))
        {
            return BadRequest();
        }

        var product = await _databaseService.GetProductById(productId);
        if (product == null)
        {
            return NotFound();
        }

        // Create a new product instance for this store
        var storeProduct = new Product
        {
            Name = product.Name,
            Category = product.Category,
            Price = product.Price,
            StoreId = storeId,
            StockQuantity = 0,
            MinimumStockLevel = product.MinimumStockLevel,
            ReorderPoint = product.ReorderPoint,
            LastUpdated = DateTime.UtcNow
        };

        await _databaseService.CreateProduct(storeProduct);
        return RedirectToAction(nameof(Index), new { storeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string storeId)
    {
        var product = await _databaseService.GetProductById(id);
        if (product == null || product.StoreId != storeId)
        {
            return NotFound();
        }

        await _databaseService.DeleteProduct(id, storeId);
        return RedirectToAction(nameof(Index), new { storeId });
    }

    public async Task<IActionResult> Search(string searchTerm, string storeId)
    {
        if (string.IsNullOrEmpty(storeId))
        {
            return RedirectToAction("Index", "Store");
        }

        var store = await _storeService.GetStoreById(storeId);
        if (store == null)
        {
            return NotFound();
        }

        var products = await _databaseService.SearchProducts(searchTerm, storeId);
        ViewBag.Store = store;
        return View("Index", products);
    }

    public async Task<IActionResult> All()
    {
        var products = await _databaseService.GetAllProducts();
        var stores = await _storeService.GetAllStores();
        var productVMs = products.Select(p => new CornerShop.Models.ProductWithStoreNameViewModel
        {
            Product = p,
            StoreName = stores.FirstOrDefault(s => s.Id == p.StoreId)?.Name ?? "(No Store)"
        }).ToList();
        return View("All", productVMs);
    }

    public async Task<IActionResult> Edit(string id, string storeId)
    {
        var product = await _databaseService.GetProductById(id);
        if (product == null || product.StoreId != storeId)
        {
            return NotFound();
        }
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product product)
    {
        if (ModelState.IsValid)
        {
            var existingProduct = await _databaseService.GetProductById(product.Id);
            if (existingProduct == null || existingProduct.StoreId != product.StoreId)
            {
                return NotFound();
            }

            // Update product properties
            existingProduct.Name = product.Name;
            existingProduct.Category = product.Category;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.MinimumStockLevel = product.MinimumStockLevel;
            existingProduct.ReorderPoint = product.ReorderPoint;
            existingProduct.LastUpdated = DateTime.UtcNow;

            await _databaseService.UpdateProduct(existingProduct);
            return RedirectToAction(nameof(Index), new { storeId = product.StoreId });
        }
        return View(product);
    }

    public async Task<IActionResult> EditGlobal(string id)
    {
        var product = await _databaseService.GetProductById(id);
        if (product == null)
            return NotFound();
        return View(product);
    }

    [HttpPost]
    // [ValidateAntiForgeryToken] // Temporarily removed for debugging
    public async Task<IActionResult> EditGlobal(Product product)
    {
        Console.WriteLine($"[DEBUG] EditGlobal POST called. Product Id: {product.Id}, Name: {product.Name}");
        if (ModelState.IsValid)
        {
            Console.WriteLine("[DEBUG] ModelState is valid.");
            var existingProduct = await _databaseService.GetProductById(product.Id);
            if (existingProduct == null)
            {
                Console.WriteLine("[DEBUG] Existing product not found.");
                return NotFound();
            }

            // Update properties...
            existingProduct.Name = product.Name;
            existingProduct.Category = product.Category;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.MinimumStockLevel = product.MinimumStockLevel;
            existingProduct.ReorderPoint = product.ReorderPoint;
            existingProduct.LastUpdated = DateTime.UtcNow;

            await _databaseService.UpdateProduct(existingProduct);
            Console.WriteLine("[DEBUG] Product updated and redirecting to All.");
            return RedirectToAction("All");
        }
        else
        {
            Console.WriteLine("[DEBUG] ModelState is NOT valid.");
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key]?.Errors;
                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"[DEBUG] ModelState error for {key}: {error.ErrorMessage}");
                    }
                }
            }
        }
        return View(product);
    }

    public IActionResult CreateGlobal()
    {
        return View("CreateGlobal", new Product());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGlobal(Product product)
    {
        if (ModelState.IsValid)
        {
            await _databaseService.CreateProduct(product);
            return RedirectToAction("All");
        }
        return View("CreateGlobal", product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGlobal(string id)
    {
        var product = await _databaseService.GetProductById(id);
        if (product != null)
        {
            await _databaseService.DeleteProduct(id, product.StoreId);
        }
        else
        {
            return NotFound();
        }
        return RedirectToAction("All");
    }
}
