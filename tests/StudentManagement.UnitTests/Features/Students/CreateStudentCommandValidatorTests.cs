using FluentAssertions;
using StudentManagement.Application.DTOs.Students;
using StudentManagement.Application.Features.Students.Commands.CreateStudent;
using Xunit;

namespace StudentManagement.UnitTests.Features.Students
{
    public class CreateStudentCommandValidatorTests
    {
        private readonly CreateStudentCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidPayload_ShouldPassValidation()
        {
            var command = new CreateStudentCommand(new CreateStudentDto
            {
                Name = "Valid Name",
                Email = "valid@example.com",
                Age = 25,
                Course = "Mechanical Engineering",
                PhoneNumber = "+1234567890"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData("", "Student name is required.")]
        [InlineData("A", "Student name must be between 2 and 100 characters.")]
        public void Validate_InvalidName_ShouldFailValidation(string invalidName, string expectedError)
        {
            var command = new CreateStudentCommand(new CreateStudentDto
            {
                Name = invalidName,
                Email = "valid@example.com",
                Age = 20,
                Course = "IT"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedError));
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("missing-at.com")]
        public void Validate_InvalidEmail_ShouldFailValidation(string invalidEmail)
        {
            var command = new CreateStudentCommand(new CreateStudentDto
            {
                Name = "Valid Name",
                Email = invalidEmail,
                Age = 20,
                Course = "IT"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Student.Email");
        }

        [Theory]
        [InlineData(4)]   // Below minimum (5)
        [InlineData(121)] // Above maximum (120)
        public void Validate_OutOfRangeAge_ShouldFailValidation(int invalidAge)
        {
            var command = new CreateStudentCommand(new CreateStudentDto
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
    }
}
