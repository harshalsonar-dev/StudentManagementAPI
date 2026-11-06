using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.DTOs.Auth
{
    public class AssignRoleDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role name is required.")]
        public string Role { get; set; } = string.Empty;
    }
}

