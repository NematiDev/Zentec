using System.Net.Http.Headers;
using System.Net.Http.Json;
using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public class PaymentClient : IPaymentClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<PaymentClient> _logger;

        public PaymentClient(HttpClient http, ILogger<PaymentClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<PaymentApiResponse<ProcessPaymentResponse>> ProcessAsync(ProcessPaymentRequest request, string bearerToken, CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "api/Payment/process")
                {
                    Content = JsonContent.Create(request)
                };

                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var res = await _http.SendAsync(req, ct);
                var payload = await res.Content.ReadFromJsonAsync<PaymentApiResponse<ProcessPaymentResponse>>(cancellationToken: ct);

                if (payload == null)
                {
                    return new PaymentApiResponse<ProcessPaymentResponse>
                    {
                        Success = false,
                        Message = "Invalid response from payment service"
                    };
                }

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling PaymentService for order {OrderId}", request.OrderId);
                return new PaymentApiResponse<ProcessPaymentResponse>
                {
                    Success = false,
                    Message = "Payment service unavailable",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
