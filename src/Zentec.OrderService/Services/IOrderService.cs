using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderResponse>> CreateOrderAsync(string userId, string userEmail, string bearerToken, CreateOrderRequest request, CancellationToken ct);
        Task<ApiResponse<OrderResponse>> GetOrderAsync(string userId, Guid orderId, CancellationToken ct);
        Task<ApiResponse<PaginatedResponse<OrderResponse>>> ListOrdersAsync(string userId, int pageNumber, int pageSize, CancellationToken ct);
        Task<ApiResponse<OrderResponse>> CancelOrderAsync(string userId, Guid orderId, string bearerToken, CancellationToken ct);
    }
}
