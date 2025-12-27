namespace Zentec.OrderService.Models.DTOs
{
    /// <summary>
    /// Request to checkout (convert cart to order)
    /// </summary>
    public class CheckoutRequest
    {
        /// <summary>
        /// Optional: Payment method ID for Stripe
        /// If not provided, a test payment method will be used
        /// </summary>
        public string? PaymentMethodId { get; set; }
    }
}