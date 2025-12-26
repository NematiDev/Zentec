using System.ComponentModel.DataAnnotations;

namespace Zentec.OrderService.Models.Entities
{
    public class CartItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CartId { get; set; }
        public Cart? Cart { get; set; }

        [MaxLength(64)]
        public string ProductId { get; set; } = string.Empty;

        [MaxLength(256)]
        public string ProductName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}