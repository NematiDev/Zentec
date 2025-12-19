using System.ComponentModel.DataAnnotations;

namespace Zentec.UserService.Models.DTOs
{
    /// <summary>
    /// Request DTO for user profile update.
    /// </summary>
    public class UpdateProfileRequest
    {
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string? FirstName { get; set; }

        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string? LastName { get; set; }

        [Phone( ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        [MaxLength(100, ErrorMessage = "Province cannot exceed 100 characters.")]
        public string? Province { get; set; }

        [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [MaxLength(255, ErrorMessage = "Address cannot exceed 100 characters.")]
        public string? Address { get; set; }

        [MaxLength(20, ErrorMessage = "Postal Code cannot exceed 20 characters.")]
        public string? PostalCode { get; set; }
    }
}
