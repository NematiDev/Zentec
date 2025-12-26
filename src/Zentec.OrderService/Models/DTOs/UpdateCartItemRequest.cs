using System.ComponentModel.DataAnnotations;

namespace Zentec.OrderService.Models.DTOs
{
    // Request to update cart item quantity
    public class UpdateCartItemRequest
    {
        [Range(0, 1000)] // 0 means remove
        public int Quantity { get; set; }
    }
}
