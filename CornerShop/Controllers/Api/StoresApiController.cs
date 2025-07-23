using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using CornerShop.Services;
using CornerShop.Models;
using Microsoft.AspNetCore.Authorization;

namespace CornerShop.Controllers.Api;

[ApiController]
[Route("api/v1/stores")]
[Produces("application/json")]
[EnableCors("ApiPolicy")]
[Authorize]
public class StoresApiController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoresApiController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    /// <summary>
    /// Get all stores with pagination, sorting, and filtering
    /// </summary>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <param name="sortBy">Sort by field (default 'Name')</param>
    /// <param name="sortOrder">Sort order: asc or desc (default 'asc')</param>
    /// <param name="searchTerm">Optional search term for filtering by name/location/address</param>
    /// <returns>List of all stores</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Store>>>> GetAllStores(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "Name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? searchTerm = null)
    {
        var stores = await _storeService.GetAllStores();
        if (stores == null)
            return Ok(new ApiResponse<IEnumerable<Store>> { Data = new List<Store>(), Links = new List<Link>() });
        // Filtering
        if (!string.IsNullOrEmpty(searchTerm))
        {
            stores = stores.Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                       s.Location.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                       s.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        // Sorting
        stores = sortBy.ToLower() switch
        {
            "name" => (sortOrder == "desc" ? stores.OrderByDescending(s => s.Name) : stores.OrderBy(s => s.Name)).ToList(),
            "location" => (sortOrder == "desc" ? stores.OrderByDescending(s => s.Location) : stores.OrderBy(s => s.Location)).ToList(),
            _ => stores.OrderBy(s => s.Name).ToList()
        };
        // Pagination
        var paged = stores.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var response = new ApiResponse<IEnumerable<Store>>
        {
            Data = paged,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetAllStores), new { page, pageSize, sortBy, sortOrder, searchTerm }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(CreateStore)) ?? "", Rel = "create", Method = "POST" }
            }
        };
        return Ok(response);
    }

    /// <summary>
    /// Get a specific store by ID
    /// </summary>
    /// <param name="id">The store ID</param>
    /// <returns>The store</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<Store>>> GetStore(string id)
    {
        var store = await _storeService.GetStoreById(id);
        if (store == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Store with ID {id} not found",
                Path = Request.Path
            });
        }

        var response = new ApiResponse<Store>
        {
            Data = store,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(GetStore), new { id }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(UpdateStore), new { id }) ?? "", Rel = "update", Method = "PUT" },
                new Link { Href = Url.Action(nameof(PatchStore), new { id }) ?? "", Rel = "patch", Method = "PATCH" },
                new Link { Href = Url.Action(nameof(DeleteStore), new { id }) ?? "", Rel = "delete", Method = "DELETE" }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new store
    /// </summary>
    /// <param name="store">The store to create</param>
    /// <returns>The created store</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Store>>> CreateStore([FromBody] Store store)
    {
        try
        {
            Console.WriteLine($"CreateStore called with store: {store?.Name}");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                Console.WriteLine($"CreateStore validation errors: {string.Join(", ", errors)}");
                return BadRequest(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 400,
                    Error = "Bad Request",
                    Message = string.Join("; ", errors),
                    Path = Request.Path
                });
            }

            if (store == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Status = 400,
                    Error = "Bad Request",
                    Message = "Store data is required",
                    Path = Request.Path
                });
            }

            if (string.IsNullOrEmpty(store.Id))
            {
                store.Id = Guid.NewGuid().ToString();
            }
            store.LastSyncTime = DateTime.UtcNow;

            Console.WriteLine($"CreateStore calling store service with ID: {store.Id}");
            await _storeService.CreateStore(store);
            Console.WriteLine($"CreateStore successfully created store: {store.Id}");

            var response = new ApiResponse<Store>
            {
                Data = store,
                Links = new List<Link>
                {
                    new Link { Href = Url.Action(nameof(GetStore), new { id = store.Id }) ?? "", Rel = "self", Method = "GET" },
                    new Link { Href = Url.Action(nameof(UpdateStore), new { id = store.Id }) ?? "", Rel = "update", Method = "PUT" },
                    new Link { Href = Url.Action(nameof(DeleteStore), new { id = store.Id }) ?? "", Rel = "delete", Method = "DELETE" }
                }
            };

            return CreatedAtAction(nameof(GetStore), new { id = store.Id }, response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateStore error: {ex.Message}");
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
    /// Update an existing store
    /// </summary>
    /// <param name="id">The store ID</param>
    /// <param name="store">The updated store data</param>
    /// <returns>No content</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStore(string id, [FromBody] Store store)
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

        var existingStore = await _storeService.GetStoreById(id);
        if (existingStore == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Store with ID {id} not found",
                Path = Request.Path
            });
        }

        // Update properties
        existingStore.Name = store.Name;
        existingStore.Location = store.Location;
        existingStore.Address = store.Address;
        existingStore.IsHeadquarters = store.IsHeadquarters;
        existingStore.Status = store.Status;

        await _storeService.UpdateStore(existingStore);
        return NoContent();
    }

    /// <summary>
    /// Partially update a store
    /// </summary>
    /// <param name="id">The store ID</param>
    /// <param name="patchData">The partial update data</param>
    /// <returns>No content</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchStore(string id, [FromBody] StorePatchRequest patchData)
    {
        var existingStore = await _storeService.GetStoreById(id);
        if (existingStore == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Store with ID {id} not found",
                Path = Request.Path
            });
        }

        // Apply partial updates
        if (patchData.Name != null) existingStore.Name = patchData.Name;
        if (patchData.Location != null) existingStore.Location = patchData.Location;
        if (patchData.Address != null) existingStore.Address = patchData.Address;
        if (patchData.IsHeadquarters.HasValue) existingStore.IsHeadquarters = patchData.IsHeadquarters.Value;
        if (patchData.Status != null) existingStore.Status = patchData.Status;

        await _storeService.UpdateStore(existingStore);
        return NoContent();
    }

    /// <summary>
    /// Delete a store
    /// </summary>
    /// <param name="id">The store ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStore(string id)
    {
        var store = await _storeService.GetStoreById(id);
        if (store == null)
        {
            return NotFound(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 404,
                Error = "Not Found",
                Message = $"Store with ID {id} not found",
                Path = Request.Path
            });
        }

        await _storeService.DeleteStore(id);
        return NoContent();
    }

    /// <summary>
    /// Search stores by name or address with pagination and sorting
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <param name="sortBy">Sort by field (default 'Name')</param>
    /// <param name="sortOrder">Sort order: asc or desc (default 'asc')</param>
    /// <returns>List of matching stores</returns>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<IEnumerable<Store>>>> SearchStores(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "Name",
        [FromQuery] string sortOrder = "asc")
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return BadRequest(new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Status = 400,
                Error = "Bad Request",
                Message = "Search term is required",
                Path = Request.Path
            });
        }
        var stores = await _storeService.GetAllStores();
        if (stores == null)
            return Ok(new ApiResponse<IEnumerable<Store>> { Data = new List<Store>(), Links = new List<Link>() });
        var matchingStores = stores.Where(s =>
            s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            s.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            s.Location.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        ).ToList();
        // Sorting
        matchingStores = sortBy.ToLower() switch
        {
            "name" => (sortOrder == "desc" ? matchingStores.OrderByDescending(s => s.Name) : matchingStores.OrderBy(s => s.Name)).ToList(),
            "location" => (sortOrder == "desc" ? matchingStores.OrderByDescending(s => s.Location) : matchingStores.OrderBy(s => s.Location)).ToList(),
            _ => matchingStores.OrderBy(s => s.Name).ToList()
        };
        // Pagination
        var paged = matchingStores.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var response = new ApiResponse<IEnumerable<Store>>
        {
            Data = paged,
            Links = new List<Link>
            {
                new Link { Href = Url.Action(nameof(SearchStores), new { searchTerm, page, pageSize, sortBy, sortOrder }) ?? "", Rel = "self", Method = "GET" },
                new Link { Href = Url.Action(nameof(GetAllStores)) ?? "", Rel = "all-stores", Method = "GET" }
            }
        };
        return Ok(response);
    }
}

public class StorePatchRequest
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? Address { get; set; }
    public bool? IsHeadquarters { get; set; }
    public string? Status { get; set; }
}
