using System.ComponentModel.DataAnnotations;

namespace Zentec.PaymentService.Models.DTOs
{
    /// <summary>
    /// Request to create a payment intent (step 1 of payment flow)
    /// </summary>
    public class CreatePaymentIntentRequest
    {
        [Required]
        public string OrderId { get; set; } = string.Empty;

        [Range(0.01, 1000000000)]
        public decimal Amount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Optional: payment method types to enable (card, us_bank_account, etc.)
        /// </summary>
        public List<string>? PaymentMethodTypes { get; set; }
    }
}
