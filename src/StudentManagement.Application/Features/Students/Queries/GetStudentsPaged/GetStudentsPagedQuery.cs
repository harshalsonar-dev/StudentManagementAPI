using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.DTOs.Students;
using StudentManagement.Domain.Interfaces;

namespace StudentManagement.Application.Features.Students.Queries.GetStudentsPaged
{
    public record GetStudentsPagedQuery(PaginationParameters Parameters) : IRequest<ApiResponse<PagedResult<StudentDto>>>;

    public class GetStudentsPagedQueryHandler : IRequestHandler<GetStudentsPagedQuery, ApiResponse<PagedResult<StudentDto>>>
    {
        private readonly IStudentRepository _studentRepository;

        public GetStudentsPagedQueryHandler(IStudentRepository studentRepository)
        {
            _studentRepository = studentRepository;
        }

        public async Task<ApiResponse<PagedResult<StudentDto>>> Handle(GetStudentsPagedQuery request, CancellationToken cancellationToken)
        {
            var p = request.Parameters;
            var (items, totalCount) = await _studentRepository.GetPagedStudentsAsync(
                p.SearchTerm,
                p.Course,
                p.IsActive,
                p.SortBy,
                p.IsAscending,
                p.PageNumber,
                p.PageSize,
                cancellationToken);

            var dtos = items.Select(s => new StudentDto
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                Age = s.Age,
                Course = s.Course,
                PhoneNumber = s.PhoneNumber,
                IsActive = s.IsActive,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate
            }).ToList();

            var pagedResult = new PagedResult<StudentDto>(dtos, totalCount, p.PageNumber, p.PageSize);
            return ApiResponse<PagedResult<StudentDto>>.SuccessResult(pagedResult, "Students retrieved successfully.");
        }
    }
}
