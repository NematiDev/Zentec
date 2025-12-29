namespace Zentec.PaymentService.Models.DTOs
{
    public class PaymentCheckoutLineItem
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal UnitAmount { get; set; }
        public int Quantity { get; set; }
    }
}
