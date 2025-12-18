namespace Zentec.UserService.Models.DTOs
{
    /// <summary>
    /// Response DTO for token refresh.
    /// </summary>
    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
