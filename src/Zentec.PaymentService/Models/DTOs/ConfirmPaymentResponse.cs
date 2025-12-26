namespace Zentec.PaymentService.Models.DTOs
{
    /// <summary>
    /// Response after payment confirmation
    /// </summary>
    public class ConfirmPaymentResponse
    {
        public bool Succeeded { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
