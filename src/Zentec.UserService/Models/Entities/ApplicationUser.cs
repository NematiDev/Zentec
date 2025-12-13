using Microsoft.AspNetCore.Identity;

namespace Zentec.UserService.Models.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
