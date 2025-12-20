namespace Zentec.ProductService.Models.DTOs
{
    public class ProductListItemResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public string? Brand { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
