using StudentManagementSystem.DTOs.Common;
using StudentManagementSystem.DTOs.Students;
using StudentManagementSystem.Models;
using StudentManagementSystem.Repositories;

namespace StudentManagementSystem.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repo;
        private readonly ILogger<StudentService> _logger;

        public StudentService(IStudentRepository repo, ILogger<StudentService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<ApiResponse<PagedResult<StudentResponseDto>>> GetAllAsync(StudentQueryParameters queryParams)
        {
            var pagedResult = await _repo.GetPagedAsync(queryParams);

            var mappedItems = pagedResult.Items.Select(MapToResponseDto);

            var result = new PagedResult<StudentResponseDto>(
                mappedItems,
                pagedResult.TotalCount,
                pagedResult.PageNumber,
                pagedResult.PageSize
            );

            return ApiResponse<PagedResult<StudentResponseDto>>.SuccessResult(result, "Students retrieved successfully.");
        }

        public async Task<ApiResponse<StudentResponseDto>> GetByIdAsync(int id)
        {
            var student = await _repo.GetByIdAsync(id);
            if (student == null)
            {
                return ApiResponse<StudentResponseDto>.FailureResult($"Student with ID {id} was not found.");
            }

            return ApiResponse<StudentResponseDto>.SuccessResult(MapToResponseDto(student), "Student found.");
        }

        public async Task<ApiResponse<StudentResponseDto>> AddAsync(StudentCreateDto dto)
        {
            var emailExists = await _repo.EmailExistsAsync(dto.Email);
            if (emailExists)
            {
                _logger.LogWarning("Attempted to add student with existing email: {Email}", dto.Email);
                return ApiResponse<StudentResponseDto>.FailureResult($"A student with email '{dto.Email}' already exists.");
            }

            var student = new Student
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Age = dto.Age,
                Course = dto.Course.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var createdStudent = await _repo.AddAsync(student);
            _logger.LogInformation("Student added with ID: {StudentId}", createdStudent.Id);

            return ApiResponse<StudentResponseDto>.SuccessResult(MapToResponseDto(createdStudent), "Student added successfully.");
        }

        public async Task<ApiResponse<StudentResponseDto>> UpdateAsync(int id, StudentUpdateDto dto)
        {
            var student = await _repo.GetByIdAsync(id);
            if (student == null)
            {
                return ApiResponse<StudentResponseDto>.FailureResult($"Student with ID {id} was not found.");
            }

            var emailExists = await _repo.EmailExistsAsync(dto.Email, id);
            if (emailExists)
            {
                return ApiResponse<StudentResponseDto>.FailureResult($"Email '{dto.Email}' is already in use by another student.");
            }

            student.Name = dto.Name.Trim();
            student.Email = dto.Email.Trim().ToLower();
            student.Age = dto.Age;
            student.Course = dto.Course.Trim();
            student.PhoneNumber = dto.PhoneNumber?.Trim();
            student.IsActive = dto.IsActive;
            student.UpdatedDate = DateTime.UtcNow;

            var updatedStudent = await _repo.UpdateAsync(student);
            _logger.LogInformation("Student updated with ID: {StudentId}", updatedStudent.Id);

            return ApiResponse<StudentResponseDto>.SuccessResult(MapToResponseDto(updatedStudent), "Student updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var deleted = await _repo.DeleteAsync(id);
            if (!deleted)
            {
                return ApiResponse<bool>.FailureResult($"Student with ID {id} was not found.");
            }

            _logger.LogInformation("Student deleted with ID: {StudentId}", id);
            return ApiResponse<bool>.SuccessResult(true, "Student deleted successfully.");
        }

        public async Task<ApiResponse<Dictionary<string, int>>> GetStatisticsAsync()
        {
            var stats = await _repo.GetCourseStatisticsAsync();
            return ApiResponse<Dictionary<string, int>>.SuccessResult(stats, "Course statistics retrieved successfully.");
        }

        private static StudentResponseDto MapToResponseDto(Student student)
        {
            return new StudentResponseDto
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                Age = student.Age,
                Course = student.Course,
                PhoneNumber = student.PhoneNumber,
                IsActive = student.IsActive,
                CreatedDate = student.CreatedDate,
                UpdatedDate = student.UpdatedDate
            };
        }
    }
}
