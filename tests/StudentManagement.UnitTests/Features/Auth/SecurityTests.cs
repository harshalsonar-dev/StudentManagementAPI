using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Features.Auth.Commands.Register;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Enums;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Infrastructure.Services;
using Moq;
using Xunit;

namespace StudentManagement.UnitTests.Features.Auth
{
    public class SecurityTests
    {
        [Theory]
        [InlineData("Admin")]
        [InlineData("Teacher")]
        [InlineData("SuperUser")]
        [InlineData("Root")]
        public async Task RegisterCommandHandler_RoleEscalationAttempt_MustAlwaysAssignStudentRole(string attemptedRole)
        {
            // Arrange: Attacker tries to register with elevated roles
            var identityServiceMock = new Mock<IIdentityService>();
            var handler = new RegisterCommandHandler(identityServiceMock.Object);

            var registerDto = new RegisterRequestDto
            {
                Email = "attacker@enterprise.edu",
                Password = "AttackerPassword123!",
                FullName = "Malicious Actor"
            };

            identityServiceMock
                .Setup(i => i.CreateUserAsync(registerDto.Email, registerDto.Password, registerDto.FullName, AppRoles.Student, It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "attacker-id", Enumerable.Empty<string>()));

            // Act
            var response = await handler.Handle(new RegisterCommand(registerDto), CancellationToken.None);

            // Assert: Must assign Student role and NEVER the attempted elevated role
            response.Success.Should().BeTrue();
            identityServiceMock.Verify(
                i => i.CreateUserAsync(registerDto.Email, registerDto.Password, registerDto.FullName, AppRoles.Student, It.IsAny<CancellationToken>()),
                Times.Once);

            identityServiceMock.Verify(
                i => i.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), attemptedRole, It.IsAny<CancellationToken>()),
                attemptedRole == AppRoles.Student ? Times.Once() : Times.Never());
        }

        [Fact]
        public void JwtToken_WhenTamperedWithInvalidKey_ShouldFailValidation()
        {
            // Arrange
            var validKey = "ValidKeyForSigningEnterpriseTokens2026_AtLeast32Bytes!";
            var wrongKey = "WrongKeyForValidatingEnterpriseTokens2026_AtLeast32Bytes!";

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user") }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = "ValidIssuer",
                Audience = "ValidAudience",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(validKey)),
                    SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Act & Assert: Validate with wrong signing key
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "ValidIssuer",
                ValidateAudience = true,
                ValidAudience = "ValidAudience",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(wrongKey)),
                ValidateLifetime = true
            };

            var act = () => tokenHandler.ValidateToken(tokenString, validationParameters, out _);
            act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
        }

        [Fact]
        public void JwtToken_WithInvalidIssuer_ShouldFailValidation()
        {
            // Arrange
            var key = "ValidKeyForSigningEnterpriseTokens2026_AtLeast32Bytes!";
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user") }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = "RogueIssuer",
                Audience = "ValidAudience",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Act & Assert: Validate expecting ValidIssuer
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "ExpectedIssuer",
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateLifetime = true
            };

            var act = () => tokenHandler.ValidateToken(tokenString, validationParameters, out _);
            act.Should().Throw<SecurityTokenInvalidIssuerException>();
        }

        [Fact]
        public void JwtToken_WithInvalidAudience_ShouldFailValidation()
        {
            // Arrange
            var key = "ValidKeyForSigningEnterpriseTokens2026_AtLeast32Bytes!";
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user") }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = "ValidIssuer",
                Audience = "RogueAudience",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Act & Assert: Validate expecting ValidAudience
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = true,
                ValidAudience = "ExpectedAudience",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateLifetime = true
            };

            var act = () => tokenHandler.ValidateToken(tokenString, validationParameters, out _);
            act.Should().Throw<SecurityTokenInvalidAudienceException>();
        }

        [Fact]
        public void HashToken_DeterministicOutput_ShouldMatchSha256Standard()
        {
            // Arrange
            var rawToken = "MySecureRefreshTokenValue123456789";

            // Act
            var hash1 = TokenService.HashToken(rawToken);
            var hash2 = TokenService.HashToken(rawToken);

            // Assert
            hash1.Should().NotBeNullOrWhiteSpace();
            hash1.Length.Should().Be(64); // SHA-256 produces 64 hex characters (256 bits)
            hash1.Should().Be(hash2); // Deterministic
            hash1.Should().NotBe(rawToken); // Non-plaintext
        }
    }
}
