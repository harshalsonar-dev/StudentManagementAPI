using FluentAssertions;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Features.Auth.Commands.Login;
using Xunit;

namespace StudentManagement.UnitTests.Features.Auth
{
    public class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidPayload_ShouldPass()
        {
            var command = new LoginCommand(new LoginRequestDto
            {
                Email = "user@enterprise.edu",
                Password = "Password123!"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_EmptyEmail_ShouldFail()
        {
            var command = new LoginCommand(new LoginRequestDto
            {
                Email = "",
                Password = "Password123!"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Dto.Email");
        }

        [Fact]
        public void Validate_InvalidEmail_ShouldFail()
        {
            var command = new LoginCommand(new LoginRequestDto
            {
                Email = "invalid-email-format",
                Password = "Password123!"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Dto.Email");
        }

        [Fact]
        public void Validate_EmptyPassword_ShouldFail()
        {
            var command = new LoginCommand(new LoginRequestDto
            {
                Email = "user@enterprise.edu",
                Password = ""
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Dto.Password");
        }
    }
}
