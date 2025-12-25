using System.ComponentModel.DataAnnotations;

namespace Zentec.OrderService.Models.DTOs
{
    public class CreateOrderRequest
    {
        [Required]
        [MinLength(1)]
        public List<CreateOrderItemRequest> Items { get; set; } = new();

        /// <summary>
        /// Optional: if true, payment service is asked to simulate failure.
        /// </summary>
        public bool SimulatePaymentFailure { get; set; } = false;
    }

    public class CreateOrderItemRequest
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int Quantity { get; set; }
    }
}
