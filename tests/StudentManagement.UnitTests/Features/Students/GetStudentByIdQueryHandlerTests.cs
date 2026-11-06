using FluentAssertions;
using Moq;
using StudentManagement.Application.Features.Students.Queries.GetStudentById;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Domain.Interfaces;
using Xunit;

namespace StudentManagement.UnitTests.Features.Students
{
    public class GetStudentByIdQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Student>> _studentRepoMock;
        private readonly GetStudentByIdQueryHandler _handler;

        public GetStudentByIdQueryHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _studentRepoMock = new Mock<IGenericRepository<Student>>();

            _unitOfWorkMock.Setup(u => u.Repository<Student>())
                           .Returns(_studentRepoMock.Object);

            _handler = new GetStudentByIdQueryHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ExistingStudentId_ShouldReturnStudentDto()
        {
            // Arrange
            var student = new Student
            {
                Id = 10,
                Name = "Jane Doe",
                Email = "jane.doe@example.com",
                Age = 21,
                Course = "Data Science",
                IsActive = true
            };

            _studentRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(student);

            var query = new GetStudentByIdQuery(10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(10);
            result.Data.Name.Should().Be("Jane Doe");
            result.Data.Email.Should().Be("jane.doe@example.com");
        }

        [Fact]
        public async Task Handle_NonExistentStudentId_ShouldThrowNotFoundException()
        {
            // Arrange
            _studentRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                            .ReturnsAsync((Student?)null);

            var query = new GetStudentByIdQuery(999);

            // Act & Assert
            var act = async () => await _handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage("*999*");
        }
    }
}
