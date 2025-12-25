using System.ComponentModel.DataAnnotations;

namespace Zentec.PaymentService.Models.DTOs
{
    public class ProcessPaymentRequest
    {
        [Required]
        public string OrderId { get; set; } = string.Empty;

        [Range(0.01, 1000000000)]
        public decimal Amount { get; set; }

        /// <summary>
        /// If true, this request will be forced to fail.
        /// </summary>
        public bool SimulateFailure { get; set; }
    }

    public class ProcessPaymentResponse
    {
        public bool Paid { get; set; }
        public string? TransactionId { get; set; }
        public string? FailureReason { get; set; }
    }
}
