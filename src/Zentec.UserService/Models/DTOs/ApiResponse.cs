namespace Zentec.UserService.Models.DTOs
{
    /// <summary>
    /// Generic API response wrapper.
    /// </summary>
    /// <typeparam name="T">type of data being returned.</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
