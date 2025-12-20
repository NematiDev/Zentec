using System.ComponentModel.DataAnnotations;

namespace Zentec.ProductService.Models.DTOs
{
    public class ProductSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsActive { get; set; } = true;
        public bool? InStock { get; set; }
        public List<string>? Tags { get; set; }

        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; } = "name";
        public bool SortDescending { get; set; } = false;
    }
}
