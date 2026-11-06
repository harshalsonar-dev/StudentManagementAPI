using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.DTOs.Students
{
    public class StudentUpdateDto
    {
        [Required(ErrorMessage = "Student name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Age is required.")]
        [Range(5, 120, ErrorMessage = "Age must be between 5 and 120.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Course is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Course must be between 2 and 100 characters.")]
        public string Course { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

