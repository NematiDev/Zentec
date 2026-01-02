using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Zentec.PaymentService.Data;
using Zentec.PaymentService.Messaging;
using Zentec.PaymentService.Models.DTOs;
using Zentec.PaymentService.Models.Entities;

namespace Zentec.PaymentService.Services
{
    public class StripePaymentService : IStripePaymentService
    {
        private readonly PaymentDbContext _db;
        private readonly ILogger<StripePaymentService> _logger;
        private readonly IConfiguration _config;
        private readonly IRabbitMqPublisher _publisher;

        public StripePaymentService(
            PaymentDbContext db,
            ILogger<StripePaymentService> logger,
            IConfiguration config,
            IRabbitMqPublisher publisher)
        {
            _db = db;
            _logger = logger;
            _config = config;

            // Set Stripe API key
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
            _publisher = publisher;
        }

        public async Task<ApiResponse<PaymentCheckoutSessionResponse>> CreateCheckoutSessionAsync(
            string userId,
            PaymentCheckoutSessionRequest request,
            CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Creating Stripe Checkout session for order {OrderId}, amount {Amount}",
                    request.OrderId, request.Amount);

                // Convert amount to cents
                var amountInCents = (long)(request.Amount * 100);

                // Build line items
                var lineItems = new List<SessionLineItemOptions>();

                if (request.LineItems != null && request.LineItems.Any())
                {
                    foreach (var item in request.LineItems)
                    {
                        lineItems.Add(new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = request.Currency.ToLower(),
                                UnitAmount = (long)(item.UnitAmount * 100),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = item.Name,
                                    Description = item.Description
                                }
                            },
                            Quantity = item.Quantity
                        });
                    }
                }
                else
                {
                    // Fallback: single line item
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.Currency.ToLower(),
                            UnitAmount = amountInCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order {request.OrderId}"
                            }
                        },
                        Quantity = 1
                    });
                }

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    CustomerEmail = request.CustomerEmail,
                    Metadata = new Dictionary<string, string>
                    {
                        { "order_id", request.OrderId },
                        { "user_id", userId }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options, cancellationToken: ct);

                // Store initial transaction record
                var transaction = new PaymentTransaction
                {
                    OrderId = request.OrderId,
                    UserId = userId,
                    Amount = amountInCents,
                    Currency = request.Currency.ToUpper(),
                    Status = PaymentStatus.Pending,
                    StripePaymentIntentId = session.PaymentIntentId,
                    IsTestPayment = false
                };

                _db.PaymentTransactions.Add(transaction);
                await _db.SaveChangesAsync(ct);

                var publishableKey = _config["Stripe:PublishableKey"] ?? "";

                return new ApiResponse<PaymentCheckoutSessionResponse>
                {
                    Success = true,
                    Message = "Checkout session created successfully",
                    Data = new PaymentCheckoutSessionResponse
                    {
                        SessionId = session.Id,
                        SessionUrl = session.Url,
                        PublishableKey = publishableKey
                    }
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating checkout session for order {OrderId}", request.OrderId);
                return new ApiResponse<PaymentCheckoutSessionResponse>
                {
                    Success = false,
                    Message = "Payment gateway error",
                    Errors = new List<string> { ex.Message }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session for order {OrderId}", request.OrderId);
                return new ApiResponse<PaymentCheckoutSessionResponse>
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
                else if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        await HandleCheckoutSessionCompletedAsync(session, ct);
                    }
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook signature verification failed");
                throw;
            }
        }

        private async Task HandleCheckoutSessionCompletedAsync(Session session, CancellationToken ct)
        {
            if (session.PaymentStatus == "paid" && session.PaymentIntentId != null)
            {
                var transaction = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(p => p.StripePaymentIntentId == session.PaymentIntentId, ct);

                if (transaction != null)
                {
                    transaction.Status = PaymentStatus.Succeeded;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);

                    _logger.LogInformation("Checkout session completed for transaction {TransactionId}", transaction.Id);
                }
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

                _publisher.PublishPaymentSucceeded(new PaymentSucceededEvent(
            OrderId: transaction.OrderId,
            PaymentIntentId: paymentIntent.Id,
            TransactionId: transaction.Id.ToString(),
            Amount: transaction.Amount / 100m,
            Currency: transaction.Currency,
            PaidAtUtc: DateTime.UtcNow
        ));

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

                _publisher.PublishPaymentFailed(new PaymentFailedEvent(
            OrderId: transaction.OrderId,
            PaymentIntentId: paymentIntent.Id,
            Reason: transaction.ErrorMessage ?? "Unknown error",
            FailedAtUtc: DateTime.UtcNow
        ));

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