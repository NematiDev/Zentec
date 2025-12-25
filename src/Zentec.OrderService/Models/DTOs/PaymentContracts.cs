namespace Zentec.OrderService.Models.DTOs
{
    public class PaymentApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool SimulateFailure { get; set; }
    }

    public class ProcessPaymentResponse
    {
        public bool Paid { get; set; }
        public string? TransactionId { get; set; }
        public string? FailureReason { get; set; }
    }
}
