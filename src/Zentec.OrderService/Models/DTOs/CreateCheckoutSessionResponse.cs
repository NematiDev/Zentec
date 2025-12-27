namespace Zentec.OrderService.Models.DTOs
{
    /// <summary>
    /// Response containing checkout session URL
    /// </summary>
    public class CreateCheckoutSessionResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
    }
}
