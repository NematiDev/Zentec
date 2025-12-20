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
        Task<ApiResponse<bool>> UpdateStockAsync(string productId, int quantity, string reason);
        Task<ApiResponse<ProductBasicResponse>> GetProductBasicInfoAsync(string productId);
        Task<ApiResponse<List<string>>> GetCategoriesAsync();
        Task<ApiResponse<List<string>>> GetBrandsAsync();
    }
}
