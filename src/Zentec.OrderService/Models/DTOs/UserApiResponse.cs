namespace Zentec.OrderService.Models.DTOs
{
    // Mirrors UserService contracts
    public class UserApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
