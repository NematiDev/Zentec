using Zentec.OrderService.Models.Entities;

namespace Zentec.OrderService.Models.DTOs
{
    // Response for cart
    public class CartResponse
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public CartStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<CartItemResponse> Items { get; set; } = new();
    }
}
