using FluentAssertions;
using Moq;
using StudentManagement.Application.Features.Students.Commands.DeleteStudent;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Interfaces;
using Xunit;

namespace StudentManagement.UnitTests.Features.Students
{
    public class DeleteStudentCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Student>> _studentRepoMock;
        private readonly DeleteStudentCommandHandler _handler;

        public DeleteStudentCommandHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _studentRepoMock = new Mock<IGenericRepository<Student>>();

            _unitOfWorkMock.Setup(u => u.Repository<Student>())
                           .Returns(_studentRepoMock.Object);

            _handler = new DeleteStudentCommandHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ExistingStudent_ShouldDeleteAndReturnSuccess()
        {
            // Arrange
            var student = new Student { Id = 5, Name = "To Delete" };
            _studentRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(student);

            var command = new DeleteStudentCommand(5);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();

            _studentRepoMock.Verify(r => r.Delete(student), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NonExistentStudent_ShouldThrowNotFoundException()
        {
            // Arrange
            _studentRepoMock.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((Student?)null);

            var command = new DeleteStudentCommand(404);

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage("*404*");

            _studentRepoMock.Verify(r => r.Delete(It.IsAny<Student>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
