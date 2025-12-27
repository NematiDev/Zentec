namespace Zentec.OrderService.Models.DTOs
{
    /// <summary>
    /// Request to create a Stripe Checkout session
    /// </summary>
    public class CreateCheckoutSessionRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// URL to redirect after successful payment
        /// </summary>
        public string SuccessUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL to redirect after cancelled payment
        /// </summary>
        public string CancelUrl { get; set; } = string.Empty;

        /// <summary>
        /// Customer email (optional)
        /// </summary>
        public string? CustomerEmail { get; set; }

        /// <summary>
        /// Product description (optional)
        /// </summary>
        public string? Description { get; set; }
    }
}
