using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using StudentManagement.Application.DTOs.Students;
using StudentManagement.Application.Features.Students.Commands.UpdateStudent;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Interfaces;
using Xunit;

namespace StudentManagement.UnitTests.Features.Students
{
    public class UpdateStudentCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Student>> _studentRepoMock;
        private readonly UpdateStudentCommandHandler _handler;

        public UpdateStudentCommandHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _studentRepoMock = new Mock<IGenericRepository<Student>>();

            _unitOfWorkMock.Setup(u => u.Repository<Student>())
                           .Returns(_studentRepoMock.Object);

            _handler = new UpdateStudentCommandHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ValidUpdate_ShouldUpdateStudentAndReturnSuccess()
        {
            // Arrange
            var existingStudent = new Student
            {
                Id = 1,
                Name = "Old Name",
                Email = "old@example.com",
                Age = 20,
                Course = "Mechanical",
                IsActive = true
            };

            _studentRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(existingStudent);

            _studentRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(false);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(1);

            var updateDto = new UpdateStudentDto
            {
                Name = "New Name",
                Email = "new@example.com",
                Age = 21,
                Course = "Computer Science",
                PhoneNumber = "+1234567890",
                IsActive = true
            };
            var command = new UpdateStudentCommand(1, updateDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("New Name");
            result.Data.Email.Should().Be("new@example.com");
            result.Data.Age.Should().Be(21);
            result.Data.Course.Should().Be("Computer Science");

            _studentRepoMock.Verify(r => r.Update(existingStudent), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NonExistentStudent_ShouldThrowNotFoundException()
        {
            // Arrange
            _studentRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((Student?)null);

            var command = new UpdateStudentCommand(999, new UpdateStudentDto
            {
                Name = "Does Not Matter",
                Email = "test@example.com",
                Age = 20,
                Course = "IT"
            });

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage("*999*");
        }

        [Fact]
        public async Task Handle_DuplicateEmailOnOtherStudent_ShouldThrowConflictException()
        {
            // Arrange
            var existingStudent = new Student
            {
                Id = 1,
                Name = "Student One",
                Email = "student1@example.com"
            };

            _studentRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(existingStudent);

            // Email "student2@example.com" belongs to student with Id = 2
            _studentRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(true);

            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = "Student One",
                Email = "student2@example.com",
                Age = 20,
                Course = "IT"
            });

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>()
                     .WithMessage("*student2@example.com*");
        }

        [Fact]
        public async Task Handle_SameEmailPreserved_ShouldNotThrowConflict()
        {
            // Arrange
            var existingStudent = new Student
            {
                Id = 1,
                Name = "Student One",
                Email = "student1@example.com",
                Age = 20,
                Course = "IT"
            };

            _studentRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(existingStudent);

            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = "Updated Name Only",
                Email = "student1@example.com", // unchanged email
                Age = 21,
                Course = "IT"
            });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.Name.Should().Be("Updated Name Only");
            _studentRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_TogglingIsActive_ShouldUpdateStatus()
        {
            // Arrange
            var existingStudent = new Student
            {
                Id = 1,
                Name = "Active Student",
                Email = "active@example.com",
                IsActive = true
            };

            _studentRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(existingStudent);

            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = "Active Student",
                Email = "active@example.com",
                Age = 22,
                Course = "IT",
                IsActive = false // Deactivating
            });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.IsActive.Should().BeFalse();
            existingStudent.IsActive.Should().BeFalse();
        }
    }
}
