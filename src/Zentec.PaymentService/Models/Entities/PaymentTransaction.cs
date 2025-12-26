using System.ComponentModel.DataAnnotations;

namespace Zentec.PaymentService.Models.Entities
{
    /// <summary>
    /// Payment transaction record
    /// </summary>
    public class PaymentTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Order ID from Order Service
        /// </summary>
        [MaxLength(64)]
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// User who made the payment
        /// </summary>
        [MaxLength(64)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Payment amount in smallest currency unit (cents for USD)
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Currency code (USD, EUR, etc.)
        /// </summary>
        [MaxLength(3)]
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Payment status
        /// </summary>
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        /// <summary>
        /// Stripe Payment Intent ID
        /// </summary>
        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        /// <summary>
        /// Stripe Charge ID (if successful)
        /// </summary>
        [MaxLength(255)]
        public string? StripeChargeId { get; set; }

        /// <summary>
        /// Payment method used (card, bank_transfer, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Last 4 digits of card (if card payment)
        /// </summary>
        [MaxLength(4)]
        public string? CardLast4 { get; set; }

        /// <summary>
        /// Card brand (Visa, Mastercard, etc.)
        /// </summary>
        [MaxLength(20)]
        public string? CardBrand { get; set; }

        /// <summary>
        /// Error message if payment failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Whether this was a simulated test payment
        /// </summary>
        public bool IsTestPayment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Processing = 1,
        Succeeded = 2,
        Failed = 3,
        Canceled = 4,
        Refunded = 5
    }
}
