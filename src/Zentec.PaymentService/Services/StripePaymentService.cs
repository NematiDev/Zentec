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
            _publisher = publisher;

            // Set Stripe API key
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
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

                // Handle checkout session events only
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        await HandleCheckoutSessionCompletedAsync(session, ct);
                    }
                }
                else if (stripeEvent.Type == "checkout.session.expired")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        await HandleCheckoutSessionExpiredAsync(session, ct);
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
            _logger.LogInformation("Processing checkout.session.completed for session {SessionId}", session.Id);

            if (session.PaymentStatus != "paid")
            {
                _logger.LogWarning("Checkout session {SessionId} payment status is {Status}, expected 'paid'",
                    session.Id, session.PaymentStatus);
                return;
            }

            // Get order ID from session metadata
            if (!session.Metadata.TryGetValue("order_id", out var orderId) || string.IsNullOrEmpty(orderId))
            {
                _logger.LogError("Checkout session {SessionId} missing order_id in metadata", session.Id);
                return;
            }

            // Find transaction by OrderId
            var transaction = await _db.PaymentTransactions
                .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

            if (transaction == null)
            {
                _logger.LogWarning("No transaction found for OrderId {OrderId}", orderId);
                return;
            }

            // Skip if already processed
            if (transaction.Status == PaymentStatus.Succeeded)
            {
                _logger.LogInformation("Transaction {TransactionId} already marked as succeeded", transaction.Id);
                return;
            }

            // Update transaction
            transaction.Status = PaymentStatus.Succeeded;
            transaction.StripePaymentIntentId = session.PaymentIntentId;
            transaction.StripeChargeId = session.Id; // Store session ID as charge reference
            transaction.PaymentMethod = session.PaymentMethodTypes?.FirstOrDefault();
            transaction.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            // ⚠️ CRITICAL: Publish to RabbitMQ so OrderService can update order status
            _publisher.PublishPaymentSucceeded(new PaymentSucceededEvent(
                OrderId: transaction.OrderId,
                PaymentIntentId: session.PaymentIntentId ?? session.Id,
                TransactionId: transaction.Id.ToString(),
                Amount: transaction.Amount / 100m,
                Currency: transaction.Currency,
                PaidAtUtc: DateTime.UtcNow
            ));

            _logger.LogInformation("✅ Payment succeeded for transaction {TransactionId}, order {OrderId}, published to RabbitMQ",
                transaction.Id, orderId);
        }

        private async Task HandleCheckoutSessionExpiredAsync(Session session, CancellationToken ct)
        {
            _logger.LogInformation("Processing checkout.session.expired for session {SessionId}", session.Id);

            // Get order ID from session metadata
            if (!session.Metadata.TryGetValue("order_id", out var orderId) || string.IsNullOrEmpty(orderId))
            {
                _logger.LogWarning("Checkout session {SessionId} missing order_id in metadata", session.Id);
                return;
            }

            var transaction = await _db.PaymentTransactions
                .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

            if (transaction == null || transaction.Status != PaymentStatus.Pending)
            {
                return;
            }

            transaction.Status = PaymentStatus.Canceled;
            transaction.ErrorMessage = "Checkout session expired";
            transaction.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _publisher.PublishPaymentFailed(new PaymentFailedEvent(
                OrderId: transaction.OrderId,
                PaymentIntentId: session.Id,
                Reason: "Checkout session expired",
                FailedAtUtc: DateTime.UtcNow
            ));

            _logger.LogWarning("❌ Checkout session expired for transaction {TransactionId}, order {OrderId}",
                transaction.Id, orderId);
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