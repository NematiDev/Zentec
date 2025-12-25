using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public interface IPaymentClient
    {
        Task<PaymentApiResponse<ProcessPaymentResponse>> ProcessAsync(ProcessPaymentRequest request, string bearerToken, CancellationToken ct);
    }
}
