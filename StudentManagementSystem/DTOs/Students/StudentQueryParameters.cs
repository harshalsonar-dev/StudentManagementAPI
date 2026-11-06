namespace StudentManagementSystem.DTOs.Students
{
    public class StudentQueryParameters
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
        }

        public string? SearchTerm { get; set; }

        public string? Course { get; set; }

        public bool? IsActive { get; set; }

        public string? SortBy { get; set; } = "Name"; // Name, Age, Course, CreatedDate

        public bool IsAscending { get; set; } = true;
    }
}

