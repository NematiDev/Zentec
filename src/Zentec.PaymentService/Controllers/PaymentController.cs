using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Zentec.PaymentService.Models.DTOs;
using Zentec.PaymentService.Services;

namespace Zentec.PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PaymentController : ControllerBase
    {
        private readonly IStripePaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IStripePaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Create a Stripe Checkout session (recommended approach)
        /// </summary>
        /// <remarks>
        /// Creates a hosted Stripe Checkout page where customers can complete payment.
        /// Returns a URL to redirect the customer to Stripe's payment page.
        /// </remarks>
        [HttpPost("create-checkout-session")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PaymentCheckoutSessionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PaymentCheckoutSessionResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] PaymentCheckoutSessionRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<PaymentCheckoutSessionResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<PaymentCheckoutSessionResponse>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var result = await _paymentService.CreateCheckoutSessionAsync(userId, request, ct);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Get a specific payment transaction
        /// </summary>
        [HttpGet("{paymentId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PaymentTransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PaymentTransactionResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPayment(Guid paymentId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<PaymentTransactionResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _paymentService.GetPaymentAsync(userId, paymentId, ct);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Get all payment transactions for the current user
        /// </summary>
        [HttpGet("my-payments")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<PaymentTransactionResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyPayments(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<List<PaymentTransactionResponse>>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _paymentService.GetUserPaymentsAsync(userId, ct);
            return Ok(result);
        }

        /// <summary>
        /// Stripe webhook endpoint
        /// </summary>
        /// <remarks>
        /// Stripe will call this endpoint when payment events occur.
        /// Configure this URL in your Stripe dashboard.
        /// </remarks>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook(CancellationToken ct)
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            try
            {
                await _paymentService.HandleWebhookAsync(json, signature, ct);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook processing failed");
                return BadRequest();
            }
        }
    }
}