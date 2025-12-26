namespace Zentec.PaymentService.Models.DTOs
{
    /// <summary>
    /// Payment transaction history response
    /// </summary>
    public class PaymentTransactionResponse
    {
        public Guid Id { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
