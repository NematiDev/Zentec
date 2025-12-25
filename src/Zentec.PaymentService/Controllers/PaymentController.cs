using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zentec.PaymentService.Models.DTOs;

namespace Zentec.PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ILogger<PaymentController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process a payment request (simulation).
        /// </summary>
        /// <remarks>
        /// This service simulates a payment gateway.
        /// - If <c>SimulateFailure=true</c> the payment fails.
        /// - Otherwise it fails randomly (~10%) to help test error paths.
        /// </remarks>
        [HttpPost("process")]
        [ProducesResponseType(typeof(ApiResponse<ProcessPaymentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProcessPaymentResponse>), StatusCodes.Status400BadRequest)]
        public IActionResult Process([FromBody] ProcessPaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<ProcessPaymentResponse>
                {
                    Success = false,
                    Message = "Invalid payment request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            try
            {
                var rnd = Random.Shared.NextDouble();
                var forcedFail = request.SimulateFailure;
                var randomFail = rnd < 0.10; // 10% random failure

                if (forcedFail || randomFail)
                {
                    var reason = forcedFail
                        ? "Simulated payment failure"
                        : "Random simulated gateway error";

                    _logger.LogWarning("Payment FAILED for Order {OrderId}. Amount: {Amount}. Reason: {Reason}", request.OrderId, request.Amount, reason);

                    return Ok(new ApiResponse<ProcessPaymentResponse>
                    {
                        Success = true,
                        Message = "Payment processed",
                        Data = new ProcessPaymentResponse
                        {
                            Paid = false,
                            FailureReason = reason
                        }
                    });
                }

                var tx = $"TX-{Guid.NewGuid():N}";
                _logger.LogInformation("Payment OK for Order {OrderId}. Amount: {Amount}. Tx: {Tx}", request.OrderId, request.Amount, tx);

                return Ok(new ApiResponse<ProcessPaymentResponse>
                {
                    Success = true,
                    Message = "Payment processed",
                    Data = new ProcessPaymentResponse
                    {
                        Paid = true,
                        TransactionId = tx
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing payment for Order {OrderId}", request.OrderId);
                return StatusCode(500, new ApiResponse<ProcessPaymentResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred"
                });
            }
        }
    }
}
