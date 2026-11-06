using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Interfaces;

namespace StudentManagement.Application.Features.Students.Commands.DeleteStudent
{
    public record DeleteStudentCommand(int Id) : IRequest<ApiResponse<bool>>;

    public class DeleteStudentCommandHandler : IRequestHandler<DeleteStudentCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteStudentCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
        {
            var studentRepo = _unitOfWork.Repository<Student>();

            var student = await studentRepo.GetByIdAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException(nameof(Student), request.Id);
            }

            studentRepo.Delete(student);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResult(true, "Student deleted successfully.");
        }
    }
}
