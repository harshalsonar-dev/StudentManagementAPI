using FluentAssertions;
using Moq;
using StudentManagement.Application.Features.Students.Queries.GetCourseStats;
using StudentManagement.Domain.Interfaces;
using Xunit;

namespace StudentManagement.UnitTests.Features.Students
{
    public class GetCourseStatsQueryHandlerTests
    {
        private readonly Mock<IStudentRepository> _studentRepoMock;
        private readonly GetCourseStatsQueryHandler _handler;

        public GetCourseStatsQueryHandlerTests()
        {
            _studentRepoMock = new Mock<IStudentRepository>();
            _handler = new GetCourseStatsQueryHandler(_studentRepoMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnCourseStatisticsDictionary()
        {
            // Arrange
            var expectedStats = new Dictionary<string, int>
            {
                { "Computer Science", 15 },
                { "Information Technology", 8 },
                { "Mechanical Engineering", 5 }
            };

            _studentRepoMock
                .Setup(r => r.GetCourseStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStats);

            var query = new GetCourseStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().ContainKey("Computer Science").WhoseValue.Should().Be(15);
            result.Data.Should().ContainKey("Information Technology").WhoseValue.Should().Be(8);
            result.Data.Should().ContainKey("Mechanical Engineering").WhoseValue.Should().Be(5);
        }

        [Fact]
        public async Task Handle_EmptyDatabase_ShouldReturnEmptyDictionary()
        {
            // Arrange
            _studentRepoMock
                .Setup(r => r.GetCourseStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, int>());

            var query = new GetCourseStatsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }
    }
}
