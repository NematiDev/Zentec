using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Zentec.OrderService.Models.DTOs;
using Zentec.OrderService.Services;

namespace Zentec.OrderService.Controllers
{
    /// <summary>
    /// Shopping cart management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's active shopping cart
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<CartResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCart(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _cartService.GetActiveCartAsync(userId, ct);
            return Ok(result);
        }

        /// <summary>
        /// Add item to cart (or update quantity if already exists)
        /// </summary>
        [HttpPost("items")]
        [ProducesResponseType(typeof(ApiResponse<CartResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CartResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var token = GetBearerToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "Bearer token missing"
                });
            }

            var result = await _cartService.AddToCartAsync(userId, token, request, ct);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Update cart item quantity (set to 0 to remove)
        /// </summary>
        [HttpPut("items/{cartItemId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CartResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CartResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCartItem(Guid cartItemId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _cartService.UpdateCartItemAsync(userId, cartItemId, request, ct);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("items/{cartItemId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CartResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CartResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveCartItem(Guid cartItemId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _cartService.RemoveCartItemAsync(userId, cartItemId, ct);
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Clear all items from cart
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearCart(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _cartService.ClearCartAsync(userId, ct);
            return Ok(result);
        }

        /// <summary>
        /// Validate user profile for checkout
        /// </summary>
        [HttpGet("validate-profile")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileValidation>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateProfile(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<UserProfileValidation>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var token = GetBearerToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new ApiResponse<UserProfileValidation>
                {
                    Success = false,
                    Message = "Bearer token missing"
                });
            }

            var validation = await _cartService.ValidateUserProfileAsync(userId, token, ct);

            return Ok(new ApiResponse<UserProfileValidation>
            {
                Success = validation.IsValid,
                Message = validation.IsValid
                    ? "Profile is complete"
                    : "Profile is incomplete",
                Data = validation
            });
        }

        /// <summary>
        /// Checkout: convert cart to order and process payment.
        /// Requires complete user profile (Province, City, Address, PostalCode).
        /// </summary>
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue("email")
                ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                ?? User.FindFirstValue("Email");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var token = GetBearerToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "Bearer token missing"
                });
            }

            var result = await _cartService.CheckoutAsync(userId, email ?? string.Empty, token, request, ct);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        private string GetBearerToken()
        {
            var authHeader = Request.Headers.Authorization.ToString();
            return authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring("Bearer ".Length).Trim()
                : string.Empty;
        }
    }
}