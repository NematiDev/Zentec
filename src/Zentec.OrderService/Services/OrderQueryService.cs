using Microsoft.EntityFrameworkCore;
using Zentec.OrderService.Data;
using Zentec.OrderService.Models.DTOs;
using Zentec.OrderService.Models.Entities;

namespace Zentec.OrderService.Services
{
    public class OrderQueryService : IOrderQueryService
    {
        private readonly OrderDbContext _db;
        private readonly IProductClient _productClient;
        private readonly ILogger<OrderQueryService> _logger;

        public OrderQueryService(
            OrderDbContext db,
            IProductClient productClient,
            ILogger<OrderQueryService> logger)
        {
            _db = db;
            _productClient = productClient;
            _logger = logger;
        }

        public async Task<ApiResponse<OrderResponse>> GetOrderAsync(string userId, Guid orderId, CancellationToken ct)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

            if (order == null)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "Order not found"
                };
            }

            return new ApiResponse<OrderResponse>
            {
                Success = true,
                Message = "Order retrieved successfully",
                Data = MapOrderToResponse(order)
            };
        }

        public async Task<ApiResponse<PaginatedResponse<OrderResponse>>> ListOrdersAsync(string userId, int pageNumber, int pageSize, CancellationToken ct)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

            var query = _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt);

            var total = await query.LongCountAsync(ct);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new ApiResponse<PaginatedResponse<OrderResponse>>
            {
                Success = true,
                Message = "Orders retrieved successfully",
                Data = new PaginatedResponse<OrderResponse>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalItems = total,
                    Items = items.Select(MapOrderToResponse).ToList()
                }
            };
        }

        public async Task<ApiResponse<OrderResponse>> CancelOrderAsync(string userId, Guid orderId, string bearerToken, CancellationToken ct)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

            if (order == null)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "Order not found"
                };
            }

            if (order.Status == OrderStatus.Paid)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "Paid orders cannot be cancelled"
                };
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = true,
                    Message = "Order already cancelled",
                    Data = MapOrderToResponse(order)
                };
            }

            // Release reserved stock (for pending/payment failed orders)
            foreach (var item in order.Items)
            {
                await _productClient.ReleaseAsync(item.ProductId, item.Quantity, bearerToken, ct);
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new ApiResponse<OrderResponse>
            {
                Success = true,
                Message = "Order cancelled successfully",
                Data = MapOrderToResponse(order)
            };
        }

        private static OrderResponse MapOrderToResponse(Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                UserEmail = order.UserEmail,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                PaymentTransactionId = order.PaymentTransactionId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.Items.Select(i => new OrderItemResponse
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal
                }).ToList()
            };
        }
    }
}