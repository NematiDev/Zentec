using Microsoft.EntityFrameworkCore;
using Zentec.OrderService.Data;
using Zentec.OrderService.Messaging;
using Zentec.OrderService.Models.DTOs;
using Zentec.OrderService.Models.Entities;

namespace Zentec.OrderService.Services
{
    public class OrderServiceImp : IOrderService
    {
        private readonly OrderDbContext _db;
        private readonly IProductClient _productClient;
        private readonly IPaymentClient _paymentClient;
        private readonly IRabbitMqPublisher _publisher;
        private readonly ILogger<OrderServiceImp> _logger;

        public OrderServiceImp(
            OrderDbContext db,
            IProductClient productClient,
            IPaymentClient paymentClient,
            IRabbitMqPublisher publisher,
            ILogger<OrderServiceImp> logger)
        {
            _db = db;
            _productClient = productClient;
            _paymentClient = paymentClient;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<ApiResponse<OrderResponse>> CreateOrderAsync(string userId, string userEmail, string bearerToken, CreateOrderRequest request, CancellationToken ct)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "Order must contain at least one item"
                };
            }

            // 1) Reserve stock in product service (compensate on failure)
            var reserved = new List<(string ProductId, int Quantity)>();
            var itemSnapshots = new List<ReserveStockResponse>();

            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ProductId) || item.Quantity <= 0)
                {
                    return new ApiResponse<OrderResponse>
                    {
                        Success = false,
                        Message = "Invalid order items"
                    };
                }

                var reserveRes = await _productClient.ReserveAsync(item.ProductId, item.Quantity, bearerToken, ct);

                if (!reserveRes.Success || reserveRes.Data == null)
                {
                    _logger.LogWarning("Reserve failed for product {ProductId}. Releasing previously reserved items.", item.ProductId);

                    // release any previous reservations
                    foreach (var r in reserved)
                    {
                        await _productClient.ReleaseAsync(r.ProductId, r.Quantity, bearerToken, ct);
                    }

                    return new ApiResponse<OrderResponse>
                    {
                        Success = false,
                        Message = "Unable to reserve product stock",
                        Errors = reserveRes.Errors ?? new List<string> { reserveRes.Message }
                    };
                }

                reserved.Add((item.ProductId, item.Quantity));
                itemSnapshots.Add(reserveRes.Data);
            }

            // 2) Create order in DB
            var order = new Order
            {
                UserId = userId,
                UserEmail = userEmail,
                Status = OrderStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var snap in itemSnapshots)
            {
                var lineTotal = snap.UnitPrice * snap.ReservedQuantity;
                order.Items.Add(new OrderItem
                {
                    ProductId = snap.ProductId,
                    ProductName = snap.ProductName,
                    UnitPrice = snap.UnitPrice,
                    Quantity = snap.ReservedQuantity,
                    LineTotal = lineTotal
                });
            }

            order.TotalAmount = order.Items.Sum(i => i.LineTotal);

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);

            // 3) Process payment
            var paymentReq = new ProcessPaymentRequest
            {
                OrderId = order.Id.ToString(),
                Amount = order.TotalAmount,
                SimulateFailure = request.SimulatePaymentFailure
            };

            var paymentRes = await _paymentClient.ProcessAsync(paymentReq, bearerToken, ct);

            if (!paymentRes.Success || paymentRes.Data == null || !paymentRes.Data.Paid)
            {
                var reason = paymentRes.Data?.FailureReason ?? paymentRes.Message;

                _logger.LogWarning("Payment failed for order {OrderId}: {Reason}", order.Id, reason);

                // Release stock reservations
                foreach (var r in reserved)
                {
                    await _productClient.ReleaseAsync(r.ProductId, r.Quantity, bearerToken, ct);
                }

                order.Status = OrderStatus.PaymentFailed;
                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                _publisher.PublishOrderPaymentFailed(new OrderPaymentFailedEvent(
                    OrderId: order.Id.ToString(),
                    UserId: userId,
                    UserEmail: userEmail,
                    TotalAmount: order.TotalAmount,
                    Reason: reason,
                    FailedAtUtc: DateTime.UtcNow));

                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "Payment failed",
                    Errors = paymentRes.Errors ?? (string.IsNullOrWhiteSpace(reason) ? null : new List<string> { reason }),
                    Data = Map(order)
                };
            }

            // 4) Mark paid and publish event
            order.Status = OrderStatus.Paid;
            order.PaymentTransactionId = paymentRes.Data.TransactionId;
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _publisher.PublishOrderPaid(new OrderPaidEvent(
                OrderId: order.Id.ToString(),
                UserId: userId,
                UserEmail: userEmail,
                TotalAmount: order.TotalAmount,
                PaymentTransactionId: order.PaymentTransactionId,
                PaidAtUtc: DateTime.UtcNow));

            return new ApiResponse<OrderResponse>
            {
                Success = true,
                Message = "Order created and paid successfully",
                Data = Map(order)
            };
        }

        public async Task<ApiResponse<OrderResponse>> GetOrderAsync(string userId, Guid orderId, CancellationToken ct)
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

            return new ApiResponse<OrderResponse>
            {
                Success = true,
                Message = "Order retrieved successfully",
                Data = Map(order)
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
                    Items = items.Select(Map).ToList()
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
                    Message = "Paid orders cannot be cancelled in this demo"
                };
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = true,
                    Message = "Order already cancelled",
                    Data = Map(order)
                };
            }

            // release reserved stock (for pending/payment failed orders, in case stock wasn't released)
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
                Data = Map(order)
            };
        }

        private static OrderResponse Map(Order order)
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
