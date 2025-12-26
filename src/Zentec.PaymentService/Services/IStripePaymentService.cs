using Zentec.PaymentService.Models.DTOs;

namespace Zentec.PaymentService.Services
{
    public interface IStripePaymentService
    {
        Task<ApiResponse<CreatePaymentIntentResponse>> CreatePaymentIntentAsync(string userId, CreatePaymentIntentRequest request, CancellationToken ct);
        Task<ApiResponse<ConfirmPaymentResponse>> ConfirmPaymentAsync(string userId, ConfirmPaymentRequest request, CancellationToken ct);
        Task<ApiResponse<PaymentTransactionResponse>> GetPaymentAsync(string userId, Guid paymentId, CancellationToken ct);
        Task<ApiResponse<List<PaymentTransactionResponse>>> GetUserPaymentsAsync(string userId, CancellationToken ct);
        Task HandleWebhookAsync(string payload, string signature, CancellationToken ct);
    }
}
