using FluentAssertions;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Features.Auth.Commands.Register;
using Xunit;

namespace StudentManagement.UnitTests.Features.Auth
{
    public class RegisterCommandValidatorTests
    {
        private readonly RegisterCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidPayload_ShouldPass()
        {
            var command = new RegisterCommand(new RegisterRequestDto
            {
                Email = "test@enterprise.edu",
                Password = "ValidPassword123!",
                FullName = "Test User",
                Role = "Student"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_EmptyEmail_ShouldFail()
        {
            var command = new RegisterCommand(new RegisterRequestDto
            {
                Email = "",
                Password = "ValidPassword123!"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Dto.Email");
        }

        [Fact]
        public void Validate_InvalidEmail_ShouldFail()
        {
            var command = new RegisterCommand(new RegisterRequestDto
            {
                Email = "not-an-email",
                Password = "ValidPassword123!"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Dto.Email");
        }

        [Fact]
        public void Validate_ShortPassword_ShouldFail()
        {
            var command = new RegisterCommand(new RegisterRequestDto
            {
                Email = "test@enterprise.edu",
                Password = "Ab1!" // Less than 8 characters
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Dto.Password");
        }

        [Fact]
        public void Validate_PasswordWithoutUppercase_ShouldFail()
        {
            var command = new RegisterCommand(new RegisterRequestDto
            {
                Email = "test@enterprise.edu",
                Password = "lowercaseonly123!"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("uppercase"));
        }

        [Fact]
        public void Validate_PasswordWithoutDigit_ShouldFail()
        {
            var command = new RegisterCommand(new RegisterRequestDto
            {
                Email = "test@enterprise.edu",
                Password = "NoDigitsHereInPass!"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("digit"));
        }

        [Fact]
        public void Validate_PasswordWithoutSpecialChar_ShouldFail()
        {
            var command = new RegisterCommand(new RegisterRequestDto
            {
                Email = "test@enterprise.edu",
                Password = "NoSpecialCharacters123"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("special character"));
        }

        [Fact]
        public void Validate_InvalidRole_ShouldFail()
        {
            var command = new RegisterCommand(new RegisterRequestDto
            {
                Email = "test@enterprise.edu",
                Password = "ValidPassword123!",
                Role = "SuperHacker" // Not in AppRoles.All
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Dto.Role");
        }
    }
}
