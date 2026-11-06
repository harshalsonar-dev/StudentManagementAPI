using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.DTOs.Students;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Interfaces;

namespace StudentManagement.Application.Features.Students.Queries.GetStudentById
{
    public record GetStudentByIdQuery(int Id) : IRequest<ApiResponse<StudentDto>>;

    public class GetStudentByIdQueryHandler : IRequestHandler<GetStudentByIdQuery, ApiResponse<StudentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetStudentByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<StudentDto>> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
        {
            var student = await _unitOfWork.Repository<Student>().GetByIdAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException(nameof(Student), request.Id);
            }

            var dto = new StudentDto
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

            return ApiResponse<StudentDto>.SuccessResult(dto, "Student retrieved successfully.");
        }
    }
}
