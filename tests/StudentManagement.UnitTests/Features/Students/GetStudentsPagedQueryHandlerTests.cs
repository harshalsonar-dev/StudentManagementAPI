using FluentAssertions;
using Moq;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.Features.Students.Queries.GetStudentsPaged;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Interfaces;
using Xunit;

namespace StudentManagement.UnitTests.Features.Students
{
    public class GetStudentsPagedQueryHandlerTests
    {
        private readonly Mock<IStudentRepository> _studentRepoMock;
        private readonly GetStudentsPagedQueryHandler _handler;

        public GetStudentsPagedQueryHandlerTests()
        {
            _studentRepoMock = new Mock<IStudentRepository>();
            _handler = new GetStudentsPagedQueryHandler(_studentRepoMock.Object);
        }

        [Fact]
        public async Task Handle_DefaultParameters_ShouldReturnPagedResult()
        {
            // Arrange
            var students = new List<Student>
            {
                new() { Id = 1, Name = "Alice", Email = "alice@example.com", Course = "CS", Age = 20 },
                new() { Id = 2, Name = "Bob", Email = "bob@example.com", Course = "IT", Age = 21 }
            };

            _studentRepoMock
                .Setup(r => r.GetPagedStudentsAsync(null, null, null, "Name", true, 1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync((students, 2));

            var query = new GetStudentsPagedQuery(new PaginationParameters { PageNumber = 1, PageSize = 10 });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
            result.Data.TotalPages.Should().Be(1);
        }

        [Fact]
        public async Task Handle_SearchTerm_ShouldFilterResults()
        {
            // Arrange
            var students = new List<Student>
            {
                new() { Id = 1, Name = "Rahul Sharma", Email = "rahul@example.com", Course = "CS", Age = 20 }
            };

            _studentRepoMock
                .Setup(r => r.GetPagedStudentsAsync("Rahul", null, null, "Name", true, 1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync((students, 1));

            var query = new GetStudentsPagedQuery(new PaginationParameters { SearchTerm = "Rahul" });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.Items.Should().ContainSingle();
            result.Data.Items.First().Name.Should().Be("Rahul Sharma");
        }

        [Fact]
        public async Task Handle_CourseFilter_ShouldFilterByCourse()
        {
            // Arrange
            var students = new List<Student>
            {
                new() { Id = 1, Name = "Alice", Email = "alice@example.com", Course = "Mechanical", Age = 22 }
            };

            _studentRepoMock
                .Setup(r => r.GetPagedStudentsAsync(null, "Mechanical", null, "Name", true, 1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync((students, 1));

            var query = new GetStudentsPagedQuery(new PaginationParameters { Course = "Mechanical" });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.Items.Should().ContainSingle();
            result.Data.Items.First().Course.Should().Be("Mechanical");
        }

        [Fact]
        public async Task Handle_IsActiveFilter_ShouldFilterByStatus()
        {
            // Arrange
            var activeStudents = new List<Student>
            {
                new() { Id = 1, Name = "Active Only", Email = "act@example.com", Course = "CS", IsActive = true }
            };

            _studentRepoMock
                .Setup(r => r.GetPagedStudentsAsync(null, null, true, "Name", true, 1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync((activeStudents, 1));

            var query = new GetStudentsPagedQuery(new PaginationParameters { IsActive = true });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.Items.Should().ContainSingle();
            result.Data.Items.First().IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_SortingParameters_ShouldPassSortingToRepo()
        {
            // Arrange
            var sortedStudents = new List<Student>
            {
                new() { Id = 3, Name = "Charlie", Age = 25 },
                new() { Id = 1, Name = "Alice", Age = 19 }
            };

            _studentRepoMock
                .Setup(r => r.GetPagedStudentsAsync(null, null, null, "Age", false, 1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync((sortedStudents, 2));

            var query = new GetStudentsPagedQuery(new PaginationParameters { SortBy = "Age", IsAscending = false });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.First().Name.Should().Be("Charlie");
        }
    }
}
