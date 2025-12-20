using MongoDB.Driver;
using Zentec.ProductService.Data;
using Zentec.ProductService.Models.DTOs;
using Zentec.ProductService.Models.Entities;

namespace Zentec.ProductService.Services
{
    public class ProductServiceImp : IProductService
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<ProductServiceImp> _logger;

        public ProductServiceImp(
            MongoDbContext context,
            ILogger<ProductServiceImp> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<ProductResponse>> CreateProductAsync(CreateProductRequest request, string userId)
        {
            try
            {
                _logger.LogInformation("Creating new product: {ProductName}", request.Name);

                var product = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    StockQuantity = request.StockQuantity,
                    Brand = request.Brand,
                    Category = request.Category,
                    ImageUrl = request.ImageUrl,
                    ImageUrls = request.ImageUrls,
                    Weight = request.Weight,
                    Tags = request.Tags,
                    Metadata = request.Metadata,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Products.InsertOneAsync(product);

                _logger.LogInformation("Product created successfully: {ProductId}", product.Id);

                return new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Product created successfully",
                    Data = MapToProductResponse(product)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", request.Name);
                return new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating the product",
                    Errors = new List<string> { "Please try again later" }
                };
            }
        }

        public async Task<ApiResponse<ProductResponse>> GetProductByIdAsync(string productId)
        {
            try
            {
                _logger.LogInformation("Retrieving product: {ProductId}", productId);

                var product = await _context.Products
                    .Find(p => p.Id == productId)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Product not found",
                        Errors = new List<string> { $"Product with ID '{productId}' does not exist" }
                    };
                }

                return new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Product retrieved successfully",
                    Data = MapToProductResponse(product)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product: {ProductId}", productId);
                return new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the product"
                };
            }
        }

        public async Task<ApiResponse<ProductResponse>> UpdateProductAsync(
            string productId,
            UpdateProductRequest request,
            string userId)
        {
            try
            {
                _logger.LogInformation("Updating product: {ProductId}", productId);

                var product = await _context.Products
                    .Find(p => p.Id == productId)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return new ApiResponse<ProductResponse>
                    {
                        Success = false,
                        Message = "Product not found"
                    };
                }

                // Build update definition
                var updateBuilder = Builders<Product>.Update;
                var updates = new List<UpdateDefinition<Product>>();

                if (!string.IsNullOrEmpty(request.Name))
                    updates.Add(updateBuilder.Set(p => p.Name, request.Name));

                if (request.Description != null)
                    updates.Add(updateBuilder.Set(p => p.Description, request.Description));

                if (request.Price.HasValue)
                    updates.Add(updateBuilder.Set(p => p.Price, request.Price.Value));

                if (request.StockQuantity.HasValue)
                    updates.Add(updateBuilder.Set(p => p.StockQuantity, request.StockQuantity.Value));

                if (request.Brand != null)
                    updates.Add(updateBuilder.Set(p => p.Brand, request.Brand));

                if (!string.IsNullOrEmpty(request.Category))
                    updates.Add(updateBuilder.Set(p => p.Category, request.Category));

                if (request.ImageUrl != null)
                    updates.Add(updateBuilder.Set(p => p.ImageUrl, request.ImageUrl));

                if (request.ImageUrls != null)
                    updates.Add(updateBuilder.Set(p => p.ImageUrls, request.ImageUrls));

                if (request.Weight.HasValue)
                    updates.Add(updateBuilder.Set(p => p.Weight, request.Weight.Value));

                if (request.Tags != null)
                    updates.Add(updateBuilder.Set(p => p.Tags, request.Tags));

                if (request.Metadata != null)
                    updates.Add(updateBuilder.Set(p => p.Metadata, request.Metadata));

                if (request.IsActive.HasValue)
                    updates.Add(updateBuilder.Set(p => p.IsActive, request.IsActive.Value));

                updates.Add(updateBuilder.Set(p => p.UpdatedAt, DateTime.UtcNow));

                var combinedUpdate = updateBuilder.Combine(updates);
                await _context.Products.UpdateOneAsync(p => p.Id == productId, combinedUpdate);

                // Fetch updated product
                var updatedProduct = await _context.Products
                    .Find(p => p.Id == productId)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Product updated successfully: {ProductId}", productId);

                return new ApiResponse<ProductResponse>
                {
                    Success = true,
                    Message = "Product updated successfully",
                    Data = MapToProductResponse(updatedProduct!)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", productId);
                return new ApiResponse<ProductResponse>
                {
                    Success = false,
                    Message = "An error occurred while updating the product"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteProductAsync(string productId)
        {
            try
            {
                _logger.LogInformation("Deleting product: {ProductId}", productId);

                var product = await _context.Products
                    .Find(p => p.Id == productId)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found"
                    };
                }

                // Soft delete - mark as inactive
                var update = Builders<Product>.Update
                    .Set(p => p.IsActive, false)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow);

                await _context.Products.UpdateOneAsync(p => p.Id == productId, update);

                _logger.LogInformation("Product deleted successfully: {ProductId}", productId);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Product deleted successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", productId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the product"
                };
            }
        }

        public async Task<ApiResponse<PaginatedResponse<ProductListItemResponse>>> SearchProductsAsync(
            ProductSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Searching products with term: {SearchTerm}", request.SearchTerm ?? "all");

                // Build filter
                var filterBuilder = Builders<Product>.Filter;
                var filters = new List<FilterDefinition<Product>>();

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchFilter = filterBuilder.Or(
                        filterBuilder.Regex(p => p.Name, new MongoDB.Bson.BsonRegularExpression(request.SearchTerm, "i")),
                        filterBuilder.Regex(p => p.Description, new MongoDB.Bson.BsonRegularExpression(request.SearchTerm, "i"))
                    );
                    filters.Add(searchFilter);
                }

                if (!string.IsNullOrWhiteSpace(request.Category))
                    filters.Add(filterBuilder.Eq(p => p.Category, request.Category));

                if (!string.IsNullOrWhiteSpace(request.Brand))
                    filters.Add(filterBuilder.Eq(p => p.Brand, request.Brand));

                if (request.MinPrice.HasValue)
                    filters.Add(filterBuilder.Gte(p => p.Price, request.MinPrice.Value));

                if (request.MaxPrice.HasValue)
                    filters.Add(filterBuilder.Lte(p => p.Price, request.MaxPrice.Value));

                if (request.IsActive.HasValue)
                    filters.Add(filterBuilder.Eq(p => p.IsActive, request.IsActive.Value));

                if (request.InStock.HasValue && request.InStock.Value)
                    filters.Add(filterBuilder.Gt(p => p.StockQuantity, 0));

                if (request.Tags != null && request.Tags.Any())
                    filters.Add(filterBuilder.AnyIn(p => p.Tags, request.Tags));

                var filter = filters.Any()
                    ? filterBuilder.And(filters)
                    : filterBuilder.Empty;

                // Get total count
                var totalCount = await _context.Products.CountDocumentsAsync(filter);

                // Build sort
                var sortBuilder = Builders<Product>.Sort;
                SortDefinition<Product> sort = request.SortBy?.ToLower() switch
                {
                    "price" => request.SortDescending
                        ? sortBuilder.Descending(p => p.Price)
                        : sortBuilder.Ascending(p => p.Price),
                    "name" => request.SortDescending
                        ? sortBuilder.Descending(p => p.Name)
                        : sortBuilder.Ascending(p => p.Name),
                    "createdat" => request.SortDescending
                        ? sortBuilder.Descending(p => p.CreatedAt)
                        : sortBuilder.Ascending(p => p.CreatedAt),
                    _ => sortBuilder.Ascending(p => p.Name)
                };

                // Get paginated data
                var products = await _context.Products
                    .Find(filter)
                    .Sort(sort)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Limit(request.PageSize)
                    .ToListAsync();

                var productList = products.Select(MapToProductListItem).ToList();

                var paginatedResponse = new PaginatedResponse<ProductListItemResponse>
                {
                    Items = productList,
                    TotalCount = (int)totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                return new ApiResponse<PaginatedResponse<ProductListItemResponse>>
                {
                    Success = true,
                    Message = "Products retrieved successfully",
                    Data = paginatedResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return new ApiResponse<PaginatedResponse<ProductListItemResponse>>
                {
                    Success = false,
                    Message = "An error occurred while searching products"
                };
            }
        }

        public async Task<ApiResponse<bool>> UpdateStockAsync(string productId, int quantity, string reason)
        {
            try
            {
                _logger.LogInformation("Updating stock for product {ProductId}: {Quantity}", productId, quantity);

                var product = await _context.Products
                    .Find(p => p.Id == productId)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found"
                    };
                }

                var newStock = product.StockQuantity + quantity;

                if (newStock < 0)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Insufficient stock",
                        Errors = new List<string> { $"Cannot reduce stock below zero. Current: {product.StockQuantity}, Requested: {quantity}" }
                    };
                }

                var update = Builders<Product>.Update
                    .Set(p => p.StockQuantity, newStock)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow);

                await _context.Products.UpdateOneAsync(p => p.Id == productId, update);

                _logger.LogInformation("Stock updated for product {ProductId}. New stock: {Stock}",
                    productId, newStock);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Stock updated successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product: {ProductId}", productId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while updating stock"
                };
            }
        }

        public async Task<ApiResponse<ProductBasicResponse>> GetProductBasicInfoAsync(string productId)
        {
            try
            {
                var product = await _context.Products
                    .Find(p => p.Id == productId)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return new ApiResponse<ProductBasicResponse>
                    {
                        Success = false,
                        Message = "Product not found"
                    };
                }

                return new ApiResponse<ProductBasicResponse>
                {
                    Success = true,
                    Message = "Product info retrieved",
                    Data = new ProductBasicResponse
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Price = product.Price,
                        StockQuantity = product.StockQuantity,
                        IsActive = product.IsActive
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product basic info: {ProductId}", productId);
                return new ApiResponse<ProductBasicResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                };
            }
        }

        public async Task<ApiResponse<List<string>>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _context.Products
                    .Distinct(p => p.Category, p => p.IsActive == true)
                    .ToListAsync();

                return new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = "Categories retrieved successfully",
                    Data = categories.OrderBy(c => c).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred"
                };
            }
        }

        public async Task<ApiResponse<List<string>>> GetBrandsAsync()
        {
            try
            {
                var brands = await _context.Products
                    .Distinct(p => p.Brand, p => p.IsActive == true && p.Brand != null)
                    .ToListAsync();

                return new ApiResponse<List<string>>
                {
                    Success = true,
                    Message = "Brands retrieved successfully",
                    Data = brands.Where(b => b != null).OrderBy(b => b).ToList()!
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brands");
                return new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred"
                };
            }
        }

        private ProductResponse MapToProductResponse(Product product)
        {
            return new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                Brand = product.Brand,
                Category = product.Category,
                ImageUrl = product.ImageUrl,
                ImageUrls = product.ImageUrls,
                Weight = product.Weight,
                Tags = product.Tags,
                Metadata = product.Metadata,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }

        private ProductListItemResponse MapToProductListItem(Product product)
        {
            return new ProductListItemResponse
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                Brand = product.Brand,
                Category = product.Category,
                ImageUrl = product.ImageUrl
            };
        }
    }
}
