using FluentValidation;
using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.DTOs.Students;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Interfaces;

namespace StudentManagement.Application.Features.Students.Commands.CreateStudent
{
    public record CreateStudentCommand(CreateStudentDto Student) : IRequest<ApiResponse<StudentDto>>;

    public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
    {
        public CreateStudentCommandValidator()
        {
            RuleFor(x => x.Student.Name)
                .NotEmpty().WithMessage("Student name is required.")
                .Length(2, 100).WithMessage("Student name must be between 2 and 100 characters.");

            RuleFor(x => x.Student.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

            RuleFor(x => x.Student.Age)
                .InclusiveBetween(5, 120).WithMessage("Age must be between 5 and 120.");

            RuleFor(x => x.Student.Course)
                .NotEmpty().WithMessage("Course is required.")
                .Length(2, 100).WithMessage("Course must be between 2 and 100 characters.");

            RuleFor(x => x.Student.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
                .Matches(@"^(\+?[0-9\s-]{7,20})?$").WithMessage("Invalid phone number format.")
                .When(x => !string.IsNullOrEmpty(x.Student.PhoneNumber));
        }
    }

    public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, ApiResponse<StudentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateStudentCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<StudentDto>> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
        {
            var studentRepo = _unitOfWork.Repository<Student>();

            // Duplicate Email Check
            var emailExists = await studentRepo.AnyAsync(s => s.Email.ToLower() == request.Student.Email.ToLower().Trim(), cancellationToken);
            if (emailExists)
            {
                throw new ConflictException($"A student with email '{request.Student.Email}' already exists.");
            }

            var student = new Student
            {
                Name = request.Student.Name.Trim(),
                Email = request.Student.Email.Trim().ToLower(),
                Age = request.Student.Age,
                Course = request.Student.Course.Trim(),
                PhoneNumber = request.Student.PhoneNumber?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await studentRepo.AddAsync(student, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var studentDto = new StudentDto
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

            return ApiResponse<StudentDto>.SuccessResult(studentDto, "Student created successfully.");
        }
    }
}
