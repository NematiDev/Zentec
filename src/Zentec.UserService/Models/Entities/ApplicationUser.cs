using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Zentec.UserService.Models.Entities
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    /// <remarks>
    /// User table also includes: Email, Password, and PhoneNumber.
    /// </remarks>
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Country {  get; set; }

        [MaxLength(100)]
        public string? Province { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }
    }
}
