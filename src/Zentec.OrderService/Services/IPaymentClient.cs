using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public interface IPaymentClient
    {
        /// <summary>
        /// Create a payment intent (step 1 - reserve payment)
        /// </summary>
        Task<PaymentApiResponse<CreatePaymentIntentResponse>> CreatePaymentIntentAsync(
            CreatePaymentIntentRequest request,
            string bearerToken,
            CancellationToken ct);

        /// <summary>
        /// Confirm a payment (step 2 - complete payment)
        /// This is typically done after collecting payment method details
        /// For simulated payments, we can use a test payment method
        /// </summary>
        Task<PaymentApiResponse<ConfirmPaymentResponse>> ConfirmPaymentAsync(
            ConfirmPaymentRequest request,
            string bearerToken,
            CancellationToken ct);
    }
}