namespace Zentec.OrderService.Models.DTOs
{
    // Request to checkout (convert cart to order)
    public class CheckoutRequest
    {
        /// <summary>
        /// Optional: if true, payment service is asked to simulate failure.
        /// </summary>
        public bool SimulatePaymentFailure { get; set; } = false;
    }
}
