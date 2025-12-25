using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Zentec.ProductService.Models.DTOs;
using Zentec.ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(
        IProductService productService,
        ILogger<ProductController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new product (Admin only).
    /// </summary>
    /// <param name="request">The product payload used to create a new product.</param>
    /// <remarks>
    /// Requires an authenticated user with the <c>Admin</c> role.
    /// The user id is taken from the JWT claim <c>NameIdentifier</c> and passed to the service for auditing.
    /// </remarks>
    /// <response code="200">Product created successfully.</response>
    /// <response code="400">Validation failed or business rules were not satisfied.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">User is authenticated but not in the Admin role.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "Invalid product data",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _productService.CreateProductAsync(request, userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateProduct endpoint");
            return StatusCode(500, new ApiResponse<ProductResponse>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Get a product by id.
    /// </summary>
    /// <param name="productId">The MongoDB id of the product.</param>
    /// <remarks>
    /// This endpoint is public (no authentication required).
    /// </remarks>
    /// <response code="200">Product found and returned.</response>
    /// <response code="404">Product was not found.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("{productId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(string productId)
    {
        try
        {
            var result = await _productService.GetProductByIdAsync(productId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", productId);
            return StatusCode(500, new ApiResponse<ProductResponse>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Update an existing product (Admin only).
    /// </summary>
    /// <param name="productId">The MongoDB id of the product to update.</param>
    /// <param name="request">The updated fields for the product.</param>
    /// <remarks>
    /// Requires an authenticated user with the <c>Admin</c> role.
    /// The user id is taken from the JWT claim <c>NameIdentifier</c> and passed to the service for auditing.
    /// </remarks>
    /// <response code="200">Product updated successfully.</response>
    /// <response code="400">Validation failed or update could not be performed.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">User is authenticated but not in the Admin role.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{productId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProduct(
        string productId,
        [FromBody] UpdateProductRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "Invalid product data",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var result = await _productService.UpdateProductAsync(productId, request, userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", productId);
            return StatusCode(500, new ApiResponse<ProductResponse>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Delete a product (Admin only).
    /// </summary>
    /// <param name="productId">The MongoDB id of the product to delete.</param>
    /// <remarks>
    /// Requires an authenticated user with the <c>Admin</c> role.
    /// </remarks>
    /// <response code="200">Product deleted successfully.</response>
    /// <response code="404">Product was not found.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">User is authenticated but not in the Admin role.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpDelete("{productId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(string productId)
    {
        try
        {
            var result = await _productService.DeleteProductAsync(productId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", productId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Search/filter products with pagination.
    /// </summary>
    /// <param name="request">Search filters and pagination settings (query string parameters).</param>
    /// <remarks>
    /// This endpoint is public (no authentication required).
    /// Use query parameters (e.g. <c>?pageNumber=1&amp;pageSize=10&amp;category=Shoes</c>).
    /// </remarks>
    /// <response code="200">Search executed successfully.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ProductListItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchRequest request)
    {
        try
        {
            var result = await _productService.SearchProductsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return StatusCode(500, new ApiResponse<PaginatedResponse<ProductListItemResponse>>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Get products (simple listing with optional category filter).
    /// </summary>
    /// <param name="pageNumber">1-based page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <param name="category">Optional category filter.</param>
    /// <remarks>
    /// This endpoint is public (no authentication required).
    /// Internally delegates to the same search logic and forces <c>IsActive=true</c>.
    /// </remarks>
    /// <response code="200">Products returned successfully.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ProductListItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null)
    {
        try
        {
            var request = new ProductSearchRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Category = category,
                IsActive = true
            };

            var result = await _productService.SearchProductsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return StatusCode(500, new ApiResponse<PaginatedResponse<ProductListItemResponse>>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Get basic product info intended for other services (e.g., Order Service).
    /// </summary>
    /// <param name="productId">The MongoDB id of the product.</param>
    /// <remarks>
    /// Requires authentication. Typically used for internal service-to-service calls.
    /// </remarks>
    /// <response code="200">Basic product data returned successfully.</response>
    /// <response code="404">Product was not found.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("{productId}/basic")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProductBasicResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductBasicInfo(string productId)
    {
        try
        {
            var result = await _productService.GetProductBasicInfoAsync(productId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting basic info for product {ProductId}", productId);
            return StatusCode(500, new ApiResponse<ProductBasicResponse>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }



    /// <summary>
    /// Reserve product stock for an order (internal use).
    /// </summary>
    /// <param name="productId">The MongoDB id of the product.</param>
    /// <param name="request">Reservation quantity (positive integer).</param>
    /// <remarks>
    /// Requires authentication. Intended for Order Service.
    /// This operation is atomic (will fail if stock is insufficient).
    /// </remarks>
    /// <response code="200">Stock reserved successfully.</response>
    /// <response code="400">Reservation failed (e.g., insufficient stock).</response>
    /// <response code="401">Authentication is required.</response>
    [HttpPost("{productId}/reserve")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<StockReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StockReservationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReserveStock(string productId, [FromBody] StockReservationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<StockReservationResponse>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var result = await _productService.ReserveStockAsync(productId, request.Quantity);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for product {ProductId}", productId);
            return StatusCode(500, new ApiResponse<StockReservationResponse>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Release previously reserved product stock (compensation).
    /// </summary>
    /// <param name="productId">The MongoDB id of the product.</param>
    /// <param name="request">Release quantity (positive integer).</param>
    /// <remarks>
    /// Requires authentication. Intended for Order Service.
    /// </remarks>
    /// <response code="200">Stock released successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Authentication is required.</response>
    [HttpPost("{productId}/release")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReleaseStock(string productId, [FromBody] StockReservationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var result = await _productService.ReleaseStockAsync(productId, request.Quantity);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing stock for product {ProductId}", productId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }
    /// <summary>
    /// Get all product categories.
    /// </summary>
    /// <remarks>
    /// This endpoint is public (no authentication required).
    /// Categories are typically derived from stored products or a predefined catalog.
    /// </remarks>
    /// <response code="200">Categories returned successfully.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var result = await _productService.GetCategoriesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, new ApiResponse<List<string>>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Get all product brands.
    /// </summary>
    /// <remarks>
    /// This endpoint is public (no authentication required).
    /// Brands are typically derived from stored products or a predefined catalog.
    /// </remarks>
    /// <response code="200">Brands returned successfully.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("brands")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrands()
    {
        try
        {
            var result = await _productService.GetBrandsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands");
            return StatusCode(500, new ApiResponse<List<string>>
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }
}
