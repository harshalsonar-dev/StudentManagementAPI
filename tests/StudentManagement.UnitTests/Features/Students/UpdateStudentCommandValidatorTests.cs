using FluentAssertions;
using StudentManagement.Application.DTOs.Students;
using StudentManagement.Application.Features.Students.Commands.UpdateStudent;
using Xunit;

namespace StudentManagement.UnitTests.Features.Students
{
    public class UpdateStudentCommandValidatorTests
    {
        private readonly UpdateStudentCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidPayload_ShouldPass()
        {
            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = "Valid Name",
                Email = "valid@example.com",
                Age = 25,
                Course = "Computer Science",
                PhoneNumber = "+1234567890",
                IsActive = true
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_InvalidId_ShouldFail(int invalidId)
        {
            var command = new UpdateStudentCommand(invalidId, new UpdateStudentDto
            {
                Name = "Valid Name",
                Email = "valid@example.com",
                Age = 20,
                Course = "IT"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Id");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_EmptyName_ShouldFail(string emptyName)
        {
            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = emptyName,
                Email = "valid@example.com",
                Age = 20,
                Course = "IT"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Student.Name");
        }

        [Fact]
        public void Validate_ShortName_ShouldFail()
        {
            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = "A",
                Email = "valid@example.com",
                Age = 20,
                Course = "IT"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Student.Name");
        }

        [Fact]
        public void Validate_InvalidEmail_ShouldFail()
        {
            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = "Valid Name",
                Email = "not-an-email",
                Age = 20,
                Course = "IT"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Student.Email");
        }

        [Theory]
        [InlineData(4)]
        [InlineData(121)]
        public void Validate_OutOfRangeAge_ShouldFail(int invalidAge)
        {
            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = "Valid Name",
                Email = "valid@example.com",
                Age = invalidAge,
                Course = "IT"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Student.Age");
        }

        [Fact]
        public void Validate_InvalidPhone_ShouldFail()
        {
            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = "Valid Name",
                Email = "valid@example.com",
                Age = 20,
                Course = "IT",
                PhoneNumber = "invalid_letters_phone"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Student.PhoneNumber");
        }

        [Fact]
        public void Validate_EmptyCourse_ShouldFail()
        {
            var command = new UpdateStudentCommand(1, new UpdateStudentDto
            {
                Name = "Valid Name",
                Email = "valid@example.com",
                Age = 20,
                Course = ""
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Student.Course");
        }
    }
}
