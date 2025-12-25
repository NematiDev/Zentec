using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Zentec.OrderService.Models.DTOs;
using Zentec.OrderService.Services;

namespace Zentec.OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new order for the current user.
        /// This endpoint reserves product stock, processes payment, stores order in PostgreSQL,
        /// and publishes an event to RabbitMQ.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue("email") ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress") ?? User.FindFirstValue("Email");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<OrderResponse> { Success = false, Message = "User not authenticated" });
            }

            // Bearer token is required for calling Product/Payment services.
            var authHeader = Request.Headers.Authorization.ToString();
            var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring("Bearer ".Length).Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new ApiResponse<OrderResponse> { Success = false, Message = "Bearer token missing" });
            }

            var result = await _orderService.CreateOrderAsync(userId, email ?? string.Empty, token, request, ct);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Get a specific order that belongs to the current user.
        /// </summary>
        [HttpGet("{orderId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(Guid orderId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<OrderResponse> { Success = false, Message = "User not authenticated" });
            }

            var result = await _orderService.GetOrderAsync(userId, orderId, ct);
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// List orders for the current user.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<OrderResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<PaginatedResponse<OrderResponse>> { Success = false, Message = "User not authenticated" });
            }

            var result = await _orderService.ListOrdersAsync(userId, pageNumber, pageSize, ct);
            return Ok(result);
        }

        /// <summary>
        /// Cancel an order (only allowed for pending or payment-failed orders).
        /// </summary>
        [HttpPost("{orderId:guid}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancel(Guid orderId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<OrderResponse> { Success = false, Message = "User not authenticated" });
            }

            var authHeader = Request.Headers.Authorization.ToString();
            var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring("Bearer ".Length).Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new ApiResponse<OrderResponse> { Success = false, Message = "Bearer token missing" });
            }

            var result = await _orderService.CancelOrderAsync(userId, orderId, token, ct);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
