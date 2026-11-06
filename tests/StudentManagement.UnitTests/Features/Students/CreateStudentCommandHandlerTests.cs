using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using StudentManagement.Application.DTOs.Students;
using StudentManagement.Application.Features.Students.Commands.CreateStudent;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Interfaces;
using Xunit;

namespace StudentManagement.UnitTests.Features.Students
{
    public class CreateStudentCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Student>> _studentRepoMock;
        private readonly CreateStudentCommandHandler _handler;

        public CreateStudentCommandHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _studentRepoMock = new Mock<IGenericRepository<Student>>();

            _unitOfWorkMock.Setup(u => u.Repository<Student>())
                           .Returns(_studentRepoMock.Object);

            _handler = new CreateStudentCommandHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateStudentAndReturnSuccess()
        {
            // Arrange
            var createDto = new CreateStudentDto
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Age = 20,
                Course = "Computer Science",
                PhoneNumber = "+1234567890"
            };
            var command = new CreateStudentCommand(createDto);

            _studentRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(false);

            _studentRepoMock.Setup(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((Student s, CancellationToken _) =>
                            {
                                s.Id = 1;
                                return s;
                            });

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(1);
            result.Data.Name.Should().Be("John Doe");
            result.Data.Email.Should().Be("john.doe@example.com");

            _studentRepoMock.Verify(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DuplicateEmail_ShouldThrowConflictException()
        {
            // Arrange
            var createDto = new CreateStudentDto
            {
                Name = "Duplicate User",
                Email = "existing@example.com",
                Age = 22,
                Course = "IT"
            };
            var command = new CreateStudentCommand(createDto);

            _studentRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(true); // Email already exists

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>()
                     .WithMessage("*existing@example.com*");

            _studentRepoMock.Verify(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
