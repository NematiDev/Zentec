using Zentec.ProductService.Models.DTOs;

namespace Zentec.ProductService.Services
{
    public interface IProductService
    {
        Task<ApiResponse<ProductResponse>> CreateProductAsync(CreateProductRequest request, string userId);
        Task<ApiResponse<ProductResponse>> GetProductByIdAsync(string productId);
        Task<ApiResponse<ProductResponse>> UpdateProductAsync(string productId, UpdateProductRequest request, string userId);
        Task<ApiResponse<bool>> DeleteProductAsync(string productId);
        Task<ApiResponse<PaginatedResponse<ProductListItemResponse>>> SearchProductsAsync(ProductSearchRequest request);

        /// <summary>
        /// Generic stock mutation endpoint (positive to increase, negative to decrease). Not atomic.
        /// Kept for backward compatibility.
        /// </summary>
        Task<ApiResponse<bool>> UpdateStockAsync(string productId, int quantity, string reason);

        /// <summary>
        /// Atomically reserve stock (decrement) only if product is active and has enough stock.
        /// Used by Order Service.
        /// </summary>
        Task<ApiResponse<StockReservationResponse>> ReserveStockAsync(string productId, int quantity);

        /// <summary>
        /// Release previously reserved stock (increment). Used as compensation.
        /// </summary>
        Task<ApiResponse<bool>> ReleaseStockAsync(string productId, int quantity);

        Task<ApiResponse<ProductBasicResponse>> GetProductBasicInfoAsync(string productId);
        Task<ApiResponse<List<string>>> GetCategoriesAsync();
        Task<ApiResponse<List<string>>> GetBrandsAsync();
    }
}
