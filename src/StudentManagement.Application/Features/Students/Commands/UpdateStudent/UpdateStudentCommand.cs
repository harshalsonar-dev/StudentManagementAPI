using FluentValidation;
using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.DTOs.Students;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Interfaces;

namespace StudentManagement.Application.Features.Students.Commands.UpdateStudent
{
    public record UpdateStudentCommand(int Id, UpdateStudentDto Student) : IRequest<ApiResponse<StudentDto>>;

    public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
    {
        public UpdateStudentCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid Student ID is required.");

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

    public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, ApiResponse<StudentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateStudentCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<StudentDto>> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
        {
            var studentRepo = _unitOfWork.Repository<Student>();

            var student = await studentRepo.GetByIdAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException(nameof(Student), request.Id);
            }

            // Check if email changed and if new email is in use
            var emailLower = request.Student.Email.Trim().ToLower();
            if (student.Email.ToLower() != emailLower)
            {
                var emailTaken = await studentRepo.AnyAsync(s => s.Email.ToLower() == emailLower && s.Id != request.Id, cancellationToken);
                if (emailTaken)
                {
                    throw new ConflictException($"Email '{request.Student.Email}' is already in use by another student.");
                }
            }

            student.Name = request.Student.Name.Trim();
            student.Email = emailLower;
            student.Age = request.Student.Age;
            student.Course = request.Student.Course.Trim();
            student.PhoneNumber = request.Student.PhoneNumber?.Trim();
            student.IsActive = request.Student.IsActive;
            student.UpdatedDate = DateTime.UtcNow;

            studentRepo.Update(student);
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

            return ApiResponse<StudentDto>.SuccessResult(studentDto, "Student updated successfully.");
        }
    }
}
