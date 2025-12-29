namespace Zentec.OrderService.Models.DTOs
{
    /// <summary>
    /// Response from checkout containing Stripe Checkout URL
    /// </summary>
    public class CheckoutResponse
    {
        public Guid OrderId { get; set; }
        public string CheckoutUrl { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }
}
