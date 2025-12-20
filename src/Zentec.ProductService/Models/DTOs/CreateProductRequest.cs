using System.ComponentModel.DataAnnotations;

namespace Zentec.ProductService.Models.DTOs
{
        public class CreateProductRequest
        {
            [Required(ErrorMessage = "Product name is required")]
            [MaxLength(200)]
            public string Name { get; set; } = string.Empty;

            [MaxLength(2000)]
            public string? Description { get; set; }

            [Required]
            [Range(0.01, 999999.99)]
            public decimal Price { get; set; }

            [Required]
            [Range(0, int.MaxValue)]
            public int StockQuantity { get; set; }

            [MaxLength(100)]
            public string? Brand { get; set; }

            [Required]
            [MaxLength(100)]
            public string Category { get; set; } = string.Empty;

            [Url]
            public string? ImageUrl { get; set; }

            public List<string>? ImageUrls { get; set; }

            [Range(0, 999.99)]
            public decimal Weight { get; set; }

            public List<string>? Tags { get; set; }

            public Dictionary<string, string>? Metadata { get; set; }
        }
    }
