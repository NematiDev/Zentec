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

        public async Task<PaymentApiResponse<PaymentCheckoutSessionResponse>> CreateCheckoutSessionAsync(
            PaymentCheckoutSessionRequest request,
            string bearerToken,
            CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "api/Payment/create-checkout-session")
                {
                    Content = JsonContent.Create(request)
                };

                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var res = await _http.SendAsync(req, ct);
                var payload = await res.Content.ReadFromJsonAsync<PaymentApiResponse<PaymentCheckoutSessionResponse>>(cancellationToken: ct);

                if (payload == null)
                {
                    return new PaymentApiResponse<PaymentCheckoutSessionResponse>
                    {
                        Success = false,
                        Message = "Invalid response from payment service"
                    };
                }

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling PaymentService create-checkout-session for order {OrderId}", request.OrderId);
                return new PaymentApiResponse<PaymentCheckoutSessionResponse>
                {
                    Success = false,
                    Message = "Payment service unavailable",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<PaymentApiResponse<CreatePaymentIntentResponse>> CreatePaymentIntentAsync(
            CreatePaymentIntentRequest request,
            string bearerToken,
            CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "api/Payment/create-intent")
                {
                    Content = JsonContent.Create(request)
                };

                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var res = await _http.SendAsync(req, ct);
                var payload = await res.Content.ReadFromJsonAsync<PaymentApiResponse<CreatePaymentIntentResponse>>(cancellationToken: ct);

                if (payload == null)
                {
                    return new PaymentApiResponse<CreatePaymentIntentResponse>
                    {
                        Success = false,
                        Message = "Invalid response from payment service"
                    };
                }

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling PaymentService create-intent for order {OrderId}", request.OrderId);
                return new PaymentApiResponse<CreatePaymentIntentResponse>
                {
                    Success = false,
                    Message = "Payment service unavailable",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<PaymentApiResponse<ConfirmPaymentResponse>> ConfirmPaymentAsync(
            ConfirmPaymentRequest request,
            string bearerToken,
            CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "api/Payment/confirm")
                {
                    Content = JsonContent.Create(request)
                };

                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var res = await _http.SendAsync(req, ct);
                var payload = await res.Content.ReadFromJsonAsync<PaymentApiResponse<ConfirmPaymentResponse>>(cancellationToken: ct);

                if (payload == null)
                {
                    return new PaymentApiResponse<ConfirmPaymentResponse>
                    {
                        Success = false,
                        Message = "Invalid response from payment service"
                    };
                }

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling PaymentService confirm for payment intent {PaymentIntentId}", request.PaymentIntentId);
                return new PaymentApiResponse<ConfirmPaymentResponse>
                {
                    Success = false,
                    Message = "Payment service unavailable",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}