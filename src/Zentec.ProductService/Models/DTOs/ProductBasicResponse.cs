namespace Zentec.ProductService.Models.DTOs
{
    public class ProductBasicResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable => IsActive && StockQuantity > 0;
    }
}
