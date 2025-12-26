namespace Zentec.OrderService.Models.DTOs
{
    // Validation result for user profile before checkout
    public class UserProfileValidation
    {
        public bool IsValid { get; set; }
        public List<string> MissingFields { get; set; } = new();
    }
}
