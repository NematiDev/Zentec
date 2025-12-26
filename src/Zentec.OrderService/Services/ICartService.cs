using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public interface ICartService
    {
        /// <summary>
        /// Get or create active cart for user
        /// </summary>
        Task<ApiResponse<CartResponse>> GetActiveCartAsync(string userId, CancellationToken ct);

        /// <summary>
        /// Add item to cart (or update quantity if already exists)
        /// </summary>
        Task<ApiResponse<CartResponse>> AddToCartAsync(string userId, string bearerToken, AddToCartRequest request, CancellationToken ct);

        /// <summary>
        /// Update cart item quantity (set to 0 to remove)
        /// </summary>
        Task<ApiResponse<CartResponse>> UpdateCartItemAsync(string userId, Guid cartItemId, UpdateCartItemRequest request, CancellationToken ct);

        /// <summary>
        /// Remove item from cart
        /// </summary>
        Task<ApiResponse<CartResponse>> RemoveCartItemAsync(string userId, Guid cartItemId, CancellationToken ct);

        /// <summary>
        /// Clear all items from cart
        /// </summary>
        Task<ApiResponse<bool>> ClearCartAsync(string userId, CancellationToken ct);

        /// <summary>
        /// Validate user profile has required fields for checkout
        /// </summary>
        Task<UserProfileValidation> ValidateUserProfileAsync(string userId, string bearerToken, CancellationToken ct);

        /// <summary>
        /// Checkout: convert cart to order and process payment
        /// </summary>
        Task<ApiResponse<OrderResponse>> CheckoutAsync(string userId, string userEmail, string bearerToken, CheckoutRequest request, CancellationToken ct);
    }
}