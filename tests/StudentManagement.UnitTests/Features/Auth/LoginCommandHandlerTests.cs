using FluentAssertions;
using Moq;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Features.Auth.Commands.Login;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Enums;
using StudentManagement.Domain.Exceptions;
using Xunit;

namespace StudentManagement.UnitTests.Features.Auth
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IIdentityService> _identityServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _identityServiceMock = new Mock<IIdentityService>();
            _tokenServiceMock = new Mock<ITokenService>();

            _handler = new LoginCommandHandler(_identityServiceMock.Object, _tokenServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCredentials_ShouldReturnAuthResponseDto()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "admin@enterprise.edu",
                Password = "ValidPassword123!"
            };
            var command = new LoginCommand(loginDto);

            var roles = new List<string> { AppRoles.Admin };

            _identityServiceMock
                .Setup(i => i.AuthenticateAsync("admin@enterprise.edu", "ValidPassword123!", It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, false, "user-123", "admin", roles));

            var expectedTokens = new AuthResponseDto
            {
                AccessToken = "sample-jwt-access-token",
                RefreshToken = "sample-secure-refresh-token",
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7),
                Email = "admin@enterprise.edu",
                UserName = "admin",
                Roles = roles
            };

            _tokenServiceMock
                .Setup(t => t.GenerateTokensAsync("user-123", "admin@enterprise.edu", "admin", roles, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTokens);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AccessToken.Should().Be("sample-jwt-access-token");
            result.Data.RefreshToken.Should().Be("sample-secure-refresh-token");
            result.Data.Roles.Should().Contain(AppRoles.Admin);
        }

        [Fact]
        public async Task Handle_InvalidCredentials_ShouldThrowUnauthorizedAppException()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "wrong@enterprise.edu",
                Password = "WrongPassword"
            };
            var command = new LoginCommand(loginDto);

            _identityServiceMock
                .Setup(i => i.AuthenticateAsync("wrong@enterprise.edu", "WrongPassword", It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, false, string.Empty, string.Empty, Array.Empty<string>()));

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAppException>()
                     .WithMessage("Invalid email or password.");

            _tokenServiceMock.Verify(
                t => t.GenerateTokensAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_AccountLockedOut_ShouldThrowUnauthorizedAppExceptionWithLockoutMessage()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Email = "locked@enterprise.edu",
                Password = "AnyPassword"
            };
            var command = new LoginCommand(loginDto);

            _identityServiceMock
                .Setup(i => i.AuthenticateAsync("locked@enterprise.edu", "AnyPassword", It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, true, "user-locked", "locked", Array.Empty<string>())); // IsLockedOut = true

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAppException>()
                     .WithMessage("*locked out*");
        }
    }
}
