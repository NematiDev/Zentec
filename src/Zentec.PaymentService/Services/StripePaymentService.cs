using Microsoft.EntityFrameworkCore;
using Stripe;
using Zentec.PaymentService.Data;
using Zentec.PaymentService.Models.DTOs;
using Zentec.PaymentService.Models.Entities;

namespace Zentec.PaymentService.Services
{
    public class StripePaymentService : IStripePaymentService
    {
        private readonly PaymentDbContext _db;
        private readonly ILogger<StripePaymentService> _logger;
        private readonly IConfiguration _config;

        public StripePaymentService(
            PaymentDbContext db,
            ILogger<StripePaymentService> logger,
            IConfiguration config)
        {
            _db = db;
            _logger = logger;
            _config = config;

            // Set Stripe API key
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }

        public async Task<ApiResponse<CreatePaymentIntentResponse>> CreatePaymentIntentAsync(
            string userId,
            CreatePaymentIntentRequest request,
            CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Creating payment intent for order {OrderId}, amount {Amount}",
                    request.OrderId, request.Amount);

                // Convert amount to cents (Stripe uses smallest currency unit)
                var amountInCents = (long)(request.Amount * 100);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = request.Currency.ToLower(),
                    PaymentMethodTypes = request.PaymentMethodTypes ?? new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "order_id", request.OrderId },
                        { "user_id", userId }
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options, cancellationToken: ct);

                // Store in database
                var transaction = new PaymentTransaction
                {
                    OrderId = request.OrderId,
                    UserId = userId,
                    Amount = amountInCents,
                    Currency = request.Currency.ToUpper(),
                    Status = PaymentStatus.Pending,
                    StripePaymentIntentId = paymentIntent.Id,
                    IsTestPayment = false
                };

                _db.PaymentTransactions.Add(transaction);
                await _db.SaveChangesAsync(ct);

                return new ApiResponse<CreatePaymentIntentResponse>
                {
                    Success = true,
                    Message = "Payment intent created successfully",
                    Data = new CreatePaymentIntentResponse
                    {
                        PaymentIntentId = paymentIntent.Id,
                        ClientSecret = paymentIntent.ClientSecret,
                        Amount = paymentIntent.Amount,
                        Currency = paymentIntent.Currency,
                        Status = paymentIntent.Status
                    }
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating payment intent for order {OrderId}", request.OrderId);
                return new ApiResponse<CreatePaymentIntentResponse>
                {
                    Success = false,
                    Message = "Payment gateway error",
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent for order {OrderId}", request.OrderId);
                return new ApiResponse<CreatePaymentIntentResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred"
                };
            }
        }

        public async Task<ApiResponse<ConfirmPaymentResponse>> ConfirmPaymentAsync(
            string userId,
            ConfirmPaymentRequest request,
            CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Confirming payment intent {PaymentIntentId}", request.PaymentIntentId);

                var options = new PaymentIntentConfirmOptions
                {
                    PaymentMethod = request.PaymentMethodId
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.ConfirmAsync(request.PaymentIntentId, options, cancellationToken: ct);

                // Update database
                var transaction = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(p => p.StripePaymentIntentId == request.PaymentIntentId, ct);

                if (transaction != null)
                {
                    transaction.Status = paymentIntent.Status == "succeeded"
                        ? PaymentStatus.Succeeded
                        : PaymentStatus.Failed;

                    if (paymentIntent.LatestCharge != null)
                    {
                        transaction.StripeChargeId = paymentIntent.LatestChargeId;
                    }

                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                }

                var succeeded = paymentIntent.Status == "succeeded";

                return new ApiResponse<ConfirmPaymentResponse>
                {
                    Success = true,
                    Message = succeeded ? "Payment successful" : "Payment failed",
                    Data = new ConfirmPaymentResponse
                    {
                        Succeeded = succeeded,
                        TransactionId = transaction?.Id.ToString(),
                        Status = paymentIntent.Status,
                        ErrorMessage = paymentIntent.LastPaymentError?.Message
                    }
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error confirming payment {PaymentIntentId}", request.PaymentIntentId);
                return new ApiResponse<ConfirmPaymentResponse>
                {
                    Success = false,
                    Message = "Payment failed",
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment {PaymentIntentId}", request.PaymentIntentId);
                return new ApiResponse<ConfirmPaymentResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred"
                };
            }
        }

        public async Task<ApiResponse<PaymentTransactionResponse>> GetPaymentAsync(
            string userId,
            Guid paymentId,
            CancellationToken ct)
        {
            var payment = await _db.PaymentTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId, ct);

            if (payment == null)
            {
                return new ApiResponse<PaymentTransactionResponse>
                {
                    Success = false,
                    Message = "Payment not found"
                };
            }

            return new ApiResponse<PaymentTransactionResponse>
            {
                Success = true,
                Message = "Payment retrieved successfully",
                Data = MapToResponse(payment)
            };
        }

        public async Task<ApiResponse<List<PaymentTransactionResponse>>> GetUserPaymentsAsync(
            string userId,
            CancellationToken ct)
        {
            var payments = await _db.PaymentTransactions
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            return new ApiResponse<List<PaymentTransactionResponse>>
            {
                Success = true,
                Message = "Payments retrieved successfully",
                Data = payments.Select(MapToResponse).ToList()
            };
        }

        public async Task HandleWebhookAsync(string payload, string signature, CancellationToken ct)
        {
            try
            {
                var webhookSecret = _config["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(
                    payload,
                    signature,
                    webhookSecret,
                    tolerance: 300,
                    throwOnApiVersionMismatch: false
                    );


                _logger.LogInformation("Received Stripe webhook: {EventType}", stripeEvent.Type);

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        await HandlePaymentSucceededAsync(paymentIntent, ct);
                    }
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        await HandlePaymentFailedAsync(paymentIntent, ct);
                    }
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook signature verification failed");
                throw;
            }
        }

        private async Task HandlePaymentSucceededAsync(PaymentIntent paymentIntent, CancellationToken ct)
        {
            var transaction = await _db.PaymentTransactions
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntent.Id, ct);

            if (transaction != null)
            {
                transaction.Status = PaymentStatus.Succeeded;
                transaction.StripeChargeId = paymentIntent.LatestChargeId;
                transaction.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Payment succeeded for transaction {TransactionId}", transaction.Id);
            }
        }

        private async Task HandlePaymentFailedAsync(PaymentIntent paymentIntent, CancellationToken ct)
        {
            var transaction = await _db.PaymentTransactions
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntent.Id, ct);

            if (transaction != null)
            {
                transaction.Status = PaymentStatus.Failed;
                transaction.ErrorMessage = paymentIntent.LastPaymentError?.Message;
                transaction.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);
                _logger.LogWarning("Payment failed for transaction {TransactionId}: {Error}",
                    transaction.Id, transaction.ErrorMessage);
            }
        }

        private static PaymentTransactionResponse MapToResponse(PaymentTransaction payment)
        {
            return new PaymentTransactionResponse
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                Amount = payment.Amount / 100m,
                Currency = payment.Currency,
                Status = payment.Status.ToString(),
                PaymentMethod = payment.PaymentMethod,
                CardLast4 = payment.CardLast4,
                CardBrand = payment.CardBrand,
                ErrorMessage = payment.ErrorMessage,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt
            };
        }
    }
}