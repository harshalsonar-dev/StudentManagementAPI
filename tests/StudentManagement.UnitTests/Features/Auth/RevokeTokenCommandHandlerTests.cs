using FluentAssertions;
using Moq;
using StudentManagement.Application.Features.Auth.Commands.RevokeToken;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Exceptions;
using Xunit;

namespace StudentManagement.UnitTests.Features.Auth
{
    public class RevokeTokenCommandHandlerTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly RevokeTokenCommandHandler _handler;

        public RevokeTokenCommandHandlerTests()
        {
            _tokenServiceMock = new Mock<ITokenService>();
            _handler = new RevokeTokenCommandHandler(_tokenServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ExistingActiveToken_ShouldRevokeAndReturnTrue()
        {
            // Arrange
            var refreshToken = "active-token-to-revoke";
            _tokenServiceMock
                .Setup(t => t.RevokeRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = new RevokeTokenCommand(refreshToken);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.Message.Should().Contain("revoked successfully");
        }

        [Fact]
        public async Task Handle_NonExistentOrAlreadyRevokedToken_ShouldThrowNotFoundException()
        {
            // Arrange
            var refreshToken = "invalid-or-already-revoked-token";
            _tokenServiceMock
                .Setup(t => t.RevokeRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var command = new RevokeTokenCommand(refreshToken);

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage("*revoked*");
        }
    }
}
