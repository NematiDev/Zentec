using System.ComponentModel.DataAnnotations;

namespace Zentec.OrderService.Models.Entities
{
    /// <summary>
    /// Shopping cart for a user (one active cart per user)
    /// </summary>
    public class Cart
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(64)]
        public string UserId { get; set; } = string.Empty;

        public CartStatus Status { get; set; } = CartStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<CartItem> Items { get; set; } = new();
    }

    public enum CartStatus
    {
        Active = 0,      // User is still shopping
        CheckedOut = 1,  // Cart converted to order
        Abandoned = 2    // User abandoned cart
    }
}