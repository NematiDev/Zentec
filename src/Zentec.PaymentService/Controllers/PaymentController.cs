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
        /// Create a payment intent (step 1 of payment flow)
        /// </summary>
        /// <remarks>
        /// Returns a client secret that should be passed to Stripe.js on the frontend.
        /// The frontend then collects payment details and confirms the payment.
        /// </remarks>
        [HttpPost("create-intent")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<CreatePaymentIntentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CreatePaymentIntentResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateIntent([FromBody] CreatePaymentIntentRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<CreatePaymentIntentResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<CreatePaymentIntentResponse>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var result = await _paymentService.CreatePaymentIntentAsync(userId, request, ct);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Confirm a payment (step 2 of payment flow)
        /// </summary>
        /// <remarks>
        /// Called after the frontend has collected payment details.
        /// Alternatively, you can confirm directly from the frontend using Stripe.js.
        /// </remarks>
        [HttpPost("confirm")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ConfirmPaymentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ConfirmPaymentResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Confirm([FromBody] ConfirmPaymentRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<ConfirmPaymentResponse>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ConfirmPaymentResponse>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var result = await _paymentService.ConfirmPaymentAsync(userId, request, ct);

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