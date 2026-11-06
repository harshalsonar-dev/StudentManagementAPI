using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Exceptions;
using StudentManagement.Infrastructure.Data;
using StudentManagement.Infrastructure.Identity;

namespace StudentManagement.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(
            IConfiguration config,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _config = config;
            _context = context;
            _userManager = userManager;
        }

        public async Task<AuthResponseDto> GenerateTokensAsync(
            string userId,
            string email,
            string userName,
            IList<string> roles,
            CancellationToken cancellationToken = default)
        {
            var key = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is missing from configuration.");
            var issuer = _config["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer is missing from configuration.");
            var audience = _config["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT Audience is missing from configuration.");

            var accessDurationMinutes = _config.GetValue<int>("Jwt:AccessTokenDurationInMinutes", 60);
            var refreshDurationDays = _config.GetValue<int>("Jwt:RefreshTokenDurationInDays", 7);

            var jti = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, userName),
                new(ClaimTypes.Email, email),
                new(JwtRegisteredClaimNames.Jti, jti)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var accessExpiration = DateTime.UtcNow.AddMinutes(accessDurationMinutes);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: accessExpiration,
                signingCredentials: credentials);

            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Generate cryptographically random refresh token
            var refreshTokenString = GenerateSecureRandomToken();
            var refreshExpiration = DateTime.UtcNow.AddDays(refreshDurationDays);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshTokenString,
                JwtId = jti,
                ExpiresOnUtc = refreshExpiration,
                CreatedOnUtc = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponseDto
            {
                AccessToken = accessTokenString,
                RefreshToken = refreshTokenString,
                AccessTokenExpiration = accessExpiration,
                RefreshTokenExpiration = refreshExpiration,
                Email = email,
                UserName = userName,
                Roles = roles
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(
            string accessToken,
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                throw new UnauthorizedAppException("Invalid access token.");
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAppException("User identifier not present in token.");
            }

            var storedRefreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

            if (storedRefreshToken == null)
            {
                throw new UnauthorizedAppException("Refresh token does not exist.");
            }

            if (storedRefreshToken.UserId != userId)
            {
                throw new UnauthorizedAppException("Refresh token does not belong to the user.");
            }

            if (storedRefreshToken.IsRevoked)
            {
                // Possible token reuse attack! Revoke all tokens for this user for security
                var allUserTokens = await _context.RefreshTokens
                    .Where(t => t.UserId == userId && t.RevokedOnUtc == null)
                    .ToListAsync(cancellationToken);

                foreach (var t in allUserTokens)
                {
                    t.RevokedOnUtc = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);

                throw new UnauthorizedAppException("Refresh token was previously revoked. Compromise suspected. Please log in again.");
            }

            if (storedRefreshToken.IsExpired)
            {
                throw new UnauthorizedAppException("Refresh token has expired. Please log in again.");
            }

            // Token Rotation: revoke current refresh token
            storedRefreshToken.RevokedOnUtc = DateTime.UtcNow;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Generate new token pair
            var newTokens = await GenerateTokensAsync(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName ?? user.Email!,
                roles,
                cancellationToken);

            storedRefreshToken.ReplacedByToken = newTokens.RefreshToken;
            await _context.SaveChangesAsync(cancellationToken);

            return newTokens;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

            if (token == null || !token.IsActive)
            {
                return false;
            }

            token.RevokedOnUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var key = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key missing.");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateLifetime = false, // We explicitly want to validate an expired token
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private static string GenerateSecureRandomToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
