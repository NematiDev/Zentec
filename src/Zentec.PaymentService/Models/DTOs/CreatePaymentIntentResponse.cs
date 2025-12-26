namespace Zentec.PaymentService.Models.DTOs
{
    /// <summary>
    /// Response with payment intent client secret
    /// </summary>
    public class CreatePaymentIntentResponse
    {
        public string PaymentIntentId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
