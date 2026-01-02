using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public interface IPaymentClient
    {
        /// <summary>
        /// Create a Stripe Checkout session
        /// </summary>
        Task<PaymentApiResponse<PaymentCheckoutSessionResponse>> CreateCheckoutSessionAsync(
            PaymentCheckoutSessionRequest request,
            string bearerToken,
            CancellationToken ct);
    }
}