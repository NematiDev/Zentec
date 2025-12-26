using System.ComponentModel.DataAnnotations;

namespace Zentec.OrderService.Models.Entities
{
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(64)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(256)]
        public string UserEmail { get; set; } = string.Empty;

        public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

        public decimal TotalAmount { get; set; }

        public string? PaymentTransactionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<OrderItem> Items { get; set; } = new();
    }

    public enum OrderStatus
    {
        PendingPayment = 0,
        Paid = 1,
        PaymentFailed = 2,
        Cancelled = 3
    }
}
