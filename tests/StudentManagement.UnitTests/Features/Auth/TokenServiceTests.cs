using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Enums;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Infrastructure.Data;
using StudentManagement.Infrastructure.Identity;
using StudentManagement.Infrastructure.Services;
using Xunit;

namespace StudentManagement.UnitTests.Features.Auth
{
    public class TokenServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;

        public TokenServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "Jwt:Key", "SuperSecretEnterpriseJwtTestingKey2026_AtLeast32BytesLong!" },
                { "Jwt:Issuer", "EnterpriseTestIssuer" },
                { "Jwt:Audience", "EnterpriseTestAudience" },
                { "Jwt:AccessTokenDurationInMinutes", "15" },
                { "Jwt:RefreshTokenDurationInDays", "7" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .Build();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _tokenService = new TokenService(_configuration, _context, _userManagerMock.Object);
        }

        [Fact]
        public async Task GenerateTokensAsync_ShouldReturnValidTokens_AndPersistRefreshToken()
        {
            // Arrange
            var userId = "user-100";
            var email = "student@enterprise.edu";
            var userName = "student";
            var roles = new List<string> { AppRoles.Student };

            // Act
            var response = await _tokenService.GenerateTokensAsync(userId, email, userName, roles);

            // Assert
            response.Should().NotBeNull();
            response.AccessToken.Should().NotBeNullOrWhiteSpace();
            response.RefreshToken.Should().NotBeNullOrWhiteSpace();
            response.Email.Should().Be(email);
            response.Roles.Should().Contain(AppRoles.Student);

            // Verify stored in DB
            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == response.RefreshToken);
            storedToken.Should().NotBeNull();
            storedToken!.UserId.Should().Be(userId);
            storedToken.IsRevoked.Should().BeFalse();
            storedToken.IsExpired.Should().BeFalse();

            // Verify JWT claims
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(response.AccessToken);
            jwt.Issuer.Should().Be("EnterpriseTestIssuer");
            jwt.Audiences.Should().Contain("EnterpriseTestAudience");
            jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
            jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == AppRoles.Student);
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidTokens_ShouldRotateTokensAndRevokeOldToken()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-200",
                Email = "teacher@enterprise.edu",
                UserName = "teacher"
            };

            _userManagerMock.Setup(u => u.FindByIdAsync("user-200"))
                            .ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user))
                            .ReturnsAsync(new List<string> { AppRoles.Teacher });

            // Generate initial tokens
            var initialTokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email, user.UserName, new List<string> { AppRoles.Teacher });

            // Act: Refresh
            var newTokens = await _tokenService.RefreshTokenAsync(initialTokens.AccessToken, initialTokens.RefreshToken);

            // Assert
            newTokens.Should().NotBeNull();
            newTokens.AccessToken.Should().NotBe(initialTokens.AccessToken);
            newTokens.RefreshToken.Should().NotBe(initialTokens.RefreshToken);

            // Verify old token is revoked
            var oldToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == initialTokens.RefreshToken);
            oldToken.Should().NotBeNull();
            oldToken!.IsRevoked.Should().BeTrue();
            oldToken.ReplacedByToken.Should().Be(newTokens.RefreshToken);

            // Verify new token exists in DB and is active
            var activeNewToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == newTokens.RefreshToken);
            activeNewToken.Should().NotBeNull();
            activeNewToken!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task RefreshTokenAsync_RevokedTokenReused_ShouldThrowUnauthorizedAndRevokeAllUserTokens()
        {
            // Arrange: simulate stolen token reuse attack
            var userId = "user-stolen";
            var initialTokens = await _tokenService.GenerateTokensAsync(userId, "victim@enterprise.edu", "victim", new List<string> { AppRoles.Student });

            // Manually revoke the token
            var tokenInDb = await _context.RefreshTokens.FirstAsync(t => t.Token == initialTokens.RefreshToken);
            tokenInDb.RevokedOnUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Act & Assert: attacker tries to use revoked token
            var act = async () => await _tokenService.RefreshTokenAsync(initialTokens.AccessToken, initialTokens.RefreshToken);

            await act.Should().ThrowAsync<UnauthorizedAppException>()
                     .WithMessage("*revoked*Compromise suspected*");
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_ShouldThrowUnauthorized()
        {
            // Arrange
            var userId = "user-expired";
            var initialTokens = await _tokenService.GenerateTokensAsync(userId, "exp@enterprise.edu", "exp", new List<string> { AppRoles.Student });

            // Set expiration in past
            var tokenInDb = await _context.RefreshTokens.FirstAsync(t => t.Token == initialTokens.RefreshToken);
            tokenInDb.ExpiresOnUtc = DateTime.UtcNow.AddMinutes(-10);
            await _context.SaveChangesAsync();

            // Act & Assert
            var act = async () => await _tokenService.RefreshTokenAsync(initialTokens.AccessToken, initialTokens.RefreshToken);

            await act.Should().ThrowAsync<UnauthorizedAppException>()
                     .WithMessage("*expired*");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
