using System.ComponentModel.DataAnnotations;

namespace Zentec.ProductService.Models.DTOs
{
    public class StockReservationRequest
    {
        [Range(1, 100000)]
        public int Quantity { get; set; }
    }

    public class StockReservationResponse
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int ReservedQuantity { get; set; }
        public int RemainingStock { get; set; }
    }
}
