namespace StudentManagement.Domain.Enums
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Teacher = "Teacher";
        public const string Student = "Student";

        public static readonly IReadOnlyList<string> All = new[] { Admin, Teacher, Student };
    }
}
