using Zentec.PaymentService.Models.DTOs;

namespace Zentec.PaymentService.Services
{
    public interface IStripePaymentService
    {
        Task<ApiResponse<PaymentTransactionResponse>> GetPaymentAsync(string userId, Guid paymentId, CancellationToken ct);
        Task<ApiResponse<List<PaymentTransactionResponse>>> GetUserPaymentsAsync(string userId, CancellationToken ct);
        Task HandleWebhookAsync(string payload, string signature, CancellationToken ct);
        Task<ApiResponse<PaymentCheckoutSessionResponse>> CreateCheckoutSessionAsync(string userId, PaymentCheckoutSessionRequest request, CancellationToken ct);
    }
}