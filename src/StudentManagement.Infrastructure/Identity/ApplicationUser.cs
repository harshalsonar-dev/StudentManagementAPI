using Microsoft.AspNetCore.Identity;

namespace StudentManagement.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
