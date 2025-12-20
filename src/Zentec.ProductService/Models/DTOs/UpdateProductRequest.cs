using System.ComponentModel.DataAnnotations;

namespace Zentec.ProductService.Models.DTOs
{
    public class UpdateProductRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0.01, 999999.99)]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [Url]
        public string? ImageUrl { get; set; }

        public List<string>? ImageUrls { get; set; }

        [Range(0, 999.99)]
        public decimal? Weight { get; set; }

        public List<string>? Tags { get; set; }

        public bool? IsActive { get; set; }

        public Dictionary<string, string>? Metadata { get; set; }
    }
}
