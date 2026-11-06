using StudentManagementSystem.DTOs.Common;
using StudentManagementSystem.DTOs.Students;

namespace StudentManagementSystem.Services
{
    public interface IStudentService
    {
        Task<ApiResponse<PagedResult<StudentResponseDto>>> GetAllAsync(StudentQueryParameters queryParams);
        Task<ApiResponse<StudentResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<StudentResponseDto>> AddAsync(StudentCreateDto dto);
        Task<ApiResponse<StudentResponseDto>> UpdateAsync(int id, StudentUpdateDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<Dictionary<string, int>>> GetStatisticsAsync();
    }
}
