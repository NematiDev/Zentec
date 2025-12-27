namespace Zentec.OrderService.Models.DTOs
{
    // Response wrapper from Payment Service
    public class PaymentApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Request to create a payment intent
    public class CreatePaymentIntentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public List<string>? PaymentMethodTypes { get; set; }
    }

    // Response from creating payment intent
    public class CreatePaymentIntentResponse
    {
        public string PaymentIntentId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // Request to confirm payment
    public class ConfirmPaymentRequest
    {
        public string PaymentIntentId { get; set; } = string.Empty;
        public string PaymentMethodId { get; set; } = string.Empty;
    }

    // Response from confirming payment
    public class ConfirmPaymentResponse
    {
        public bool Succeeded { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}