using Microsoft.EntityFrameworkCore;
using Zentec.OrderService.Data;
using Zentec.OrderService.Messaging;
using Zentec.OrderService.Models.DTOs;
using Zentec.OrderService.Models.Entities;

namespace Zentec.OrderService.Services
{
    public class CartService : ICartService
    {
        private readonly OrderDbContext _db;
        private readonly IProductClient _productClient;
        private readonly IPaymentClient _paymentClient;
        private readonly IUserClient _userClient;
        private readonly IRabbitMqPublisher _publisher;
        private readonly ILogger<CartService> _logger;

        public CartService(
            OrderDbContext db,
            IProductClient productClient,
            IPaymentClient paymentClient,
            IUserClient userClient,
            IRabbitMqPublisher publisher,
            ILogger<CartService> logger)
        {
            _db = db;
            _productClient = productClient;
            _paymentClient = paymentClient;
            _userClient = userClient;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<ApiResponse<CartResponse>> GetActiveCartAsync(string userId, CancellationToken ct)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    Status = CartStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Carts.Add(cart);
                await SaveChangesWithRetryAsync(ct);

                cart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstAsync(c => c.Id == cart.Id, ct);
            }

            return new ApiResponse<CartResponse>
            {
                Success = true,
                Message = "Cart retrieved successfully",
                Data = MapCartToResponse(cart)
            };
        }

        public async Task<ApiResponse<CartResponse>> AddToCartAsync(string userId, string bearerToken, AddToCartRequest request, CancellationToken ct)
        {
            var productResult = await _productClient.GetBasicInfoAsync(request.ProductId, bearerToken, ct);
            if (!productResult.Success || productResult.Data == null)
            {
                return new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "Product not found or unavailable",
                    Errors = productResult.Errors
                };
            }

            var product = productResult.Data;
            if (!product.IsAvailable)
            {
                return new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "Product is not available"
                };
            }

            var cart = await _db.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    Status = CartStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Carts.Add(cart);
                await SaveChangesWithRetryAsync(ct);
            }

            var canonicalProductId = product.Id;

            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == canonicalProductId, ct);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                existingItem.LineTotal = existingItem.UnitPrice * existingItem.Quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = canonicalProductId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = request.Quantity,
                    LineTotal = product.Price * request.Quantity,
                    AddedAt = DateTime.UtcNow
                };

                _db.CartItems.Add(newItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await SaveChangesWithRetryAsync(ct);

            var reloadedCart = await _db.Carts
                .Include(c => c.Items)
                .FirstAsync(c => c.Id == cart.Id, ct);

            return new ApiResponse<CartResponse>
            {
                Success = true,
                Message = "Item added to cart",
                Data = MapCartToResponse(reloadedCart)
            };
        }

        public async Task<ApiResponse<CartResponse>> UpdateCartItemAsync(string userId, Guid cartItemId, UpdateCartItemRequest request, CancellationToken ct)
        {
            var cart = await _db.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);

            if (cart == null)
            {
                return new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "Cart not found"
                };
            }

            var item = await _db.CartItems
                .FirstOrDefaultAsync(i => i.Id == cartItemId && i.CartId == cart.Id, ct);

            if (item == null)
            {
                return new ApiResponse<CartResponse>
                {
                    Success = false,
                    Message = "Cart item not found"
                };
            }

            if (request.Quantity == 0)
            {
                _db.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = request.Quantity;
                item.LineTotal = item.UnitPrice * item.Quantity;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await SaveChangesWithRetryAsync(ct);

            var reloadedCart = await _db.Carts
                .Include(c => c.Items)
                .FirstAsync(c => c.Id == cart.Id, ct);

            return new ApiResponse<CartResponse>
            {
                Success = true,
                Message = "Cart updated",
                Data = MapCartToResponse(reloadedCart)
            };
        }

        public async Task<ApiResponse<CartResponse>> RemoveCartItemAsync(string userId, Guid cartItemId, CancellationToken ct)
        {
            return await UpdateCartItemAsync(userId, cartItemId, new UpdateCartItemRequest { Quantity = 0 }, ct);
        }

        public async Task<ApiResponse<bool>> ClearCartAsync(string userId, CancellationToken ct)
        {
            var cart = await _db.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);

            if (cart == null)
            {
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "No active cart found",
                    Data = true
                };
            }

            var items = await _db.CartItems
                .Where(i => i.CartId == cart.Id)
                .ToListAsync(ct);

            if (items.Count > 0)
                _db.CartItems.RemoveRange(items);

            cart.UpdatedAt = DateTime.UtcNow;
            await SaveChangesWithRetryAsync(ct);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Cart cleared",
                Data = true
            };
        }

        public async Task<UserProfileValidation> ValidateUserProfileAsync(string userId, string bearerToken, CancellationToken ct)
        {
            var profileResult = await _userClient.GetUserProfileAsync(bearerToken, ct);

            if (!profileResult.Success || profileResult.Data == null)
            {
                return new UserProfileValidation
                {
                    IsValid = false,
                    MissingFields = new List<string> { "Unable to retrieve user profile" }
                };
            }

            var profile = profileResult.Data;
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(profile.Province))
                missingFields.Add("Province");

            if (string.IsNullOrWhiteSpace(profile.City))
                missingFields.Add("City");

            if (string.IsNullOrWhiteSpace(profile.Address))
                missingFields.Add("Address");

            if (string.IsNullOrWhiteSpace(profile.PostalCode))
                missingFields.Add("PostalCode");

            return new UserProfileValidation
            {
                IsValid = missingFields.Count == 0,
                MissingFields = missingFields
            };
        }

        public async Task<ApiResponse<OrderResponse>> CheckoutAsync(string userId, string userEmail, string bearerToken, CheckoutRequest request, CancellationToken ct)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);

            if (cart == null || cart.Items == null || cart.Items.Count == 0)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "Cart is empty"
                };
            }

            var validation = await ValidateUserProfileAsync(userId, bearerToken, ct);
            if (!validation.IsValid)
            {
                return new ApiResponse<OrderResponse>
                {
                    Success = false,
                    Message = "User profile is incomplete",
                    Errors = validation.MissingFields.Select(f => $"{f} is required").ToList()
                };
            }

            var reserved = new List<(string ProductId, int Quantity)>();

            foreach (var item in cart.Items)
            {
                var reserveRes = await _productClient.ReserveAsync(item.ProductId, item.Quantity, bearerToken, ct);

                if (!reserveRes.Success || reserveRes.Data == null)
                {
                    foreach (var r in reserved)
                        await _productClient.ReleaseAsync(r.ProductId, r.Quantity, bearerToken, ct);

                    return new ApiResponse<OrderResponse>
                    {
                        Success = false,
                        Message = "Unable to reserve product stock",
                        Errors = reserveRes.Errors ?? new List<string> { reserveRes.Message }
                    };
                }

                reserved.Add((item.ProductId, item.Quantity));
            }

            var order = new Order
            {
                UserId = userId,
                UserEmail = userEmail,
                Status = OrderStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>()
            };

            foreach (var item in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.LineTotal
                });
            }

            order.TotalAmount = order.Items.Sum(i => i.LineTotal);
            _db.Orders.Add(order);

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

                foreach (var r in reserved)
                    await _productClient.ReleaseAsync(r.ProductId, r.Quantity, bearerToken, ct);

                order.Status = OrderStatus.PaymentFailed;
                order.UpdatedAt = DateTime.UtcNow;

                await SaveChangesWithRetryAsync(ct);

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
                    Data = MapOrderToResponse(order)
                };
            }

            order.Status = OrderStatus.Paid;
            order.PaymentTransactionId = paymentRes.Data.TransactionId;
            order.UpdatedAt = DateTime.UtcNow;

            cart.Status = CartStatus.CheckedOut;
            cart.UpdatedAt = DateTime.UtcNow;

            await SaveChangesWithRetryAsync(ct);

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
                Data = MapOrderToResponse(order)
            };
        }

        private async Task SaveChangesWithRetryAsync(CancellationToken ct)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict detected. Retrying once.");

                foreach (var entry in ex.Entries)
                    await entry.ReloadAsync(ct);

                await _db.SaveChangesAsync(ct);
            }
        }

        private static CartResponse MapCartToResponse(Cart cart)
        {
            cart.Items ??= new List<CartItem>();

            return new CartResponse
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Status = cart.Status,
                TotalAmount = cart.Items.Sum(i => i.LineTotal),
                TotalItems = cart.Items.Sum(i => i.Quantity),
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                Items = cart.Items.Select(i => new CartItemResponse
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal,
                    AddedAt = i.AddedAt
                }).ToList()
            };
        }

        private static OrderResponse MapOrderToResponse(Order order)
        {
            order.Items ??= new List<OrderItem>();

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
