using StudentManagementSystem.DTOs.Common;
using StudentManagementSystem.DTOs.Students;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Repositories
{
    public interface IStudentRepository
    {
        Task<IEnumerable<Student>> GetAllAsync();
        Task<PagedResult<Student>> GetPagedAsync(StudentQueryParameters queryParams);
        Task<Student?> GetByIdAsync(int id);
        Task<Student?> GetByEmailAsync(string email);
        Task<bool> ExistsAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task<Student> AddAsync(Student student);
        Task<Student> UpdateAsync(Student student);
        Task<bool> DeleteAsync(int id);
        Task<Dictionary<string, int>> GetCourseStatisticsAsync();
    }
}
