using StudentManagement.Domain.Entities;

namespace StudentManagement.Domain.Interfaces
{
    public interface IStudentRepository : IGenericRepository<Student>
    {
        Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPagedStudentsAsync(
            string? searchTerm,
            string? course,
            bool? isActive,
            string? sortBy,
            bool isAscending,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<Dictionary<string, int>> GetCourseStatisticsAsync(CancellationToken cancellationToken = default);
    }
}
