using System.Net.Http.Headers;
using System.Net.Http.Json;
using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public class ProductClient : IProductClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ProductClient> _logger;

        public ProductClient(HttpClient http, ILogger<ProductClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<ProductApiResponse<ReserveStockResponse>> ReserveAsync(string productId, int quantity, string bearerToken, CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, $"api/Product/{productId}/reserve")
                {
                    Content = JsonContent.Create(new ReserveStockRequest { Quantity = quantity })
                };

                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var res = await _http.SendAsync(req, ct);
                var payload = await res.Content.ReadFromJsonAsync<ProductApiResponse<ReserveStockResponse>>(cancellationToken: ct);

                if (payload == null)
                {
                    return new ProductApiResponse<ReserveStockResponse>
                    {
                        Success = false,
                        Message = "Invalid response from product service"
                    };
                }

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ProductService reserve for {ProductId}", productId);
                return new ProductApiResponse<ReserveStockResponse>
                {
                    Success = false,
                    Message = "Product service unavailable",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ProductApiResponse<bool>> ReleaseAsync(string productId, int quantity, string bearerToken, CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, $"api/Product/{productId}/release")
                {
                    Content = JsonContent.Create(new ReserveStockRequest { Quantity = quantity })
                };

                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var res = await _http.SendAsync(req, ct);
                var payload = await res.Content.ReadFromJsonAsync<ProductApiResponse<bool>>(cancellationToken: ct);

                if (payload == null)
                {
                    return new ProductApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid response from product service"
                    };
                }

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ProductService release for {ProductId}", productId);
                return new ProductApiResponse<bool>
                {
                    Success = false,
                    Message = "Product service unavailable",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
