using StudentManagement.Domain.Common;

namespace StudentManagement.Domain.Entities
{
    public class Student : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Course { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
