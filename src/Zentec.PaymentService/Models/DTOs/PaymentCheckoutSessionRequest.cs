namespace Zentec.PaymentService.Models.DTOs
{
    /// <summary>
    /// Request to create a Stripe Checkout session
    /// </summary>
    public class PaymentCheckoutSessionRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public List<PaymentCheckoutLineItem>? LineItems { get; set; }
    }
}
