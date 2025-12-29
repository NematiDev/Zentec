namespace Zentec.PaymentService.Models.DTOs
{
    /// <summary>
    /// Response containing checkout session details
    /// </summary>
    public class PaymentCheckoutSessionResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string SessionUrl { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
    }
}
