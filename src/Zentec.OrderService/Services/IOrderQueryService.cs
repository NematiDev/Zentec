using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    /// <summary>
    /// Service for querying and managing existing orders (not creating new ones)
    /// </summary>
    public interface IOrderQueryService
    {
        /// <summary>
        /// Get a specific order by ID
        /// </summary>
        Task<ApiResponse<OrderResponse>> GetOrderAsync(string userId, Guid orderId, CancellationToken ct);

        /// <summary>
        /// List orders for a user with pagination
        /// </summary>
        Task<ApiResponse<PaginatedResponse<OrderResponse>>> ListOrdersAsync(string userId, int pageNumber, int pageSize, CancellationToken ct);

        /// <summary>
        /// Cancel an order (releases stock if needed)
        /// </summary>
        Task<ApiResponse<OrderResponse>> CancelOrderAsync(string userId, Guid orderId, string bearerToken, CancellationToken ct);
    }
}