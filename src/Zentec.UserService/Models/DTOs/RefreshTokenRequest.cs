using System.ComponentModel.DataAnnotations;

namespace Zentec.UserService.Models.DTOs
{
    /// <summary>
    /// Request DTO for refreshing access token (JWT)
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Access token is required.")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
