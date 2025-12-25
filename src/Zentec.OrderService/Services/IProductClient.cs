using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public interface IProductClient
    {
        Task<ProductApiResponse<ReserveStockResponse>> ReserveAsync(string productId, int quantity, string bearerToken, CancellationToken ct);
        Task<ProductApiResponse<bool>> ReleaseAsync(string productId, int quantity, string bearerToken, CancellationToken ct);
    }
}
