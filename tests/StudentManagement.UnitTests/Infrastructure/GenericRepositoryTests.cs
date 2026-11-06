using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Domain.Entities;
using StudentManagement.Infrastructure.Data;
using StudentManagement.Infrastructure.Repositories;
using Xunit;

namespace StudentManagement.UnitTests.Infrastructure
{
    public class GenericRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GenericRepository<Student> _repository;

        public GenericRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new GenericRepository<Student>(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldInsertEntity()
        {
            // Arrange
            var student = new Student
            {
                Name = "Repo Test",
                Email = "repo@test.com",
                Course = "CS",
                Age = 22
            };

            // Act
            var added = await _repository.AddAsync(student);
            await _context.SaveChangesAsync();

            // Assert
            added.Id.Should().BeGreaterThan(0);
            var inDb = await _context.Students.FindAsync(added.Id);
            inDb.Should().NotBeNull();
            inDb!.Name.Should().Be("Repo Test");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldRetrieveEntity()
        {
            // Arrange
            var student = new Student
            {
                Name = "Lookup Test",
                Email = "lookup@test.com",
                Course = "IT",
                Age = 20
            };
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();

            // Act
            var retrieved = await _repository.GetByIdAsync(student.Id);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Email.Should().Be("lookup@test.com");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllEntities()
        {
            // Arrange
            await _context.Students.AddRangeAsync(
                new Student { Name = "S1", Email = "s1@test.com", Course = "CS", Age = 20 },
                new Student { Name = "S2", Email = "s2@test.com", Course = "CS", Age = 21 }
            );
            await _context.SaveChangesAsync();

            // Act
            var all = await _repository.GetAllAsync();

            // Assert
            all.Should().HaveCount(2);
        }

        [Fact]
        public async Task FindAsync_ShouldFilterEntities()
        {
            // Arrange
            await _context.Students.AddRangeAsync(
                new Student { Name = "Find CS", Email = "cs@test.com", Course = "CS", Age = 20 },
                new Student { Name = "Find IT", Email = "it@test.com", Course = "IT", Age = 21 }
            );
            await _context.SaveChangesAsync();

            // Act
            var csOnly = await _repository.FindAsync(s => s.Course == "CS");

            // Assert
            csOnly.Should().ContainSingle();
            csOnly.First().Name.Should().Be("Find CS");
        }

        [Fact]
        public async Task CountAsync_ShouldReturnAccurateCount()
        {
            // Arrange
            await _context.Students.AddRangeAsync(
                new Student { Name = "Count 1", Email = "c1@test.com", Course = "CS", Age = 20 },
                new Student { Name = "Count 2", Email = "c2@test.com", Course = "IT", Age = 21 },
                new Student { Name = "Count 3", Email = "c3@test.com", Course = "CS", Age = 22 }
            );
            await _context.SaveChangesAsync();

            // Act
            var totalCount = await _repository.CountAsync();
            var csCount = await _repository.CountAsync(s => s.Course == "CS");

            // Assert
            totalCount.Should().Be(3);
            csCount.Should().Be(2);
        }

        [Fact]
        public async Task Delete_ShouldRemoveEntity()
        {
            // Arrange
            var student = new Student
            {
                Name = "Delete Me",
                Email = "del@test.com",
                Course = "CS",
                Age = 20
            };
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();

            // Act
            _repository.Delete(student);
            await _context.SaveChangesAsync();

            // Assert
            var inDb = await _context.Students.FindAsync(student.Id);
            inDb.Should().BeNull();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
