using System.ComponentModel.DataAnnotations;

namespace Zentec.PaymentService.Models.DTOs
{
    /// <summary>
    /// Request to confirm a payment (step 2 of payment flow)
    /// </summary>
    public class ConfirmPaymentRequest
    {
        [Required]
        public string PaymentIntentId { get; set; } = string.Empty;

        [Required]
        public string PaymentMethodId { get; set; } = string.Empty;
    }
}
