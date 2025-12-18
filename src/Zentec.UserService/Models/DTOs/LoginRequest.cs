using System.ComponentModel.DataAnnotations;

namespace Zentec.UserService.Models.DTOs
{
    /// <summary>
    /// Request DTO for user login.
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(7, ErrorMessage = "Password must be at least 7 characters.")]
        public string Password { get; set; } = string.Empty;
    }
}
