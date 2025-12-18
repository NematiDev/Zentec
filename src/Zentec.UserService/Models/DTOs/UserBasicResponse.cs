using System.Globalization;

namespace Zentec.UserService.Models.DTOs
{
    /// <summary>
    /// Response DTO for basic user information.
    /// </summary>
    public class UserBasicResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
