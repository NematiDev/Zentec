namespace Zentec.UserService.Models.DTOs
{
    /// <summary>
    /// Response DTO for login and registration opeartions.
    /// </summary>
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserBasicResponse User { get; set; } = null!;
    }
}
