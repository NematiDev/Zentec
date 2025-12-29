namespace Zentec.OrderService.Models.DTOs
{
    /// <summary>
    /// Request to checkout (convert cart to order)
    /// </summary>
    public class CheckoutRequest
    {
        /// <summary>
        /// Success URL to redirect after successful payment
        /// </summary>
        public string SuccessUrl { get; set; } = string.Empty;

        /// <summary>
        /// Cancel URL to redirect after cancelled payment
        /// </summary>
        public string CancelUrl { get; set; } = string.Empty;
    }
}