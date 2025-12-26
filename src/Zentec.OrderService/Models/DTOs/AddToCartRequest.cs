using System.ComponentModel.DataAnnotations;

namespace Zentec.OrderService.Models.DTOs
{
    // Request to add item to cart
    public class AddToCartRequest
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int Quantity { get; set; }
    }
}
