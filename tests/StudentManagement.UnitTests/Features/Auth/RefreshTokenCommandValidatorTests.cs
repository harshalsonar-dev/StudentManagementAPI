using FluentAssertions;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Features.Auth.Commands.RefreshToken;
using Xunit;

namespace StudentManagement.UnitTests.Features.Auth
{
    public class RefreshTokenCommandValidatorTests
    {
        private readonly RefreshTokenCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidPayload_ShouldPass()
        {
            var command = new RefreshTokenCommand(new RefreshTokenRequestDto
            {
                AccessToken = "valid-access-token",
                RefreshToken = "valid-refresh-token"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_EmptyAccessToken_ShouldFail()
        {
            var command = new RefreshTokenCommand(new RefreshTokenRequestDto
            {
                AccessToken = "",
                RefreshToken = "valid-refresh-token"
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Dto.AccessToken");
        }

        [Fact]
        public void Validate_EmptyRefreshToken_ShouldFail()
        {
            var command = new RefreshTokenCommand(new RefreshTokenRequestDto
            {
                AccessToken = "valid-access-token",
                RefreshToken = ""
            });

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Dto.RefreshToken");
        }
    }
}
