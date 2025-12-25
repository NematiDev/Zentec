namespace Zentec.OrderService.Models.DTOs
{
    // Mirrors ProductService contracts used in inter-service calls.
    public class ProductApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ProductBasicResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable => IsActive && StockQuantity > 0;
    }

    public class ReserveStockRequest
    {
        public int Quantity { get; set; }
    }

    public class ReserveStockResponse
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int ReservedQuantity { get; set; }
        public int RemainingStock { get; set; }
    }
}
