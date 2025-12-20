namespace Zentec.ProductService.Models.DTOs
{
    public class ProductResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public string? Brand { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; }
        public decimal Weight { get; set; }
        public List<string>? Tags { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
