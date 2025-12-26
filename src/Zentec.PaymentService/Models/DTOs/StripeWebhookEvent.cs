namespace Zentec.PaymentService.Models.DTOs
{
    /// <summary>
    /// Webhook event from Stripe
    /// </summary>
    public class StripeWebhookEvent
    {
        public string Type { get; set; } = string.Empty;
        public object Data { get; set; } = new();
    }
}
