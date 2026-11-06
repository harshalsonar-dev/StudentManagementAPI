using System.Security.Claims;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Domain.Entities;

namespace StudentManagement.Application.Interfaces
{
    public interface ITokenService
    {
        Task<AuthResponseDto> GenerateTokensAsync(string userId, string email, string userName, IList<string> roles, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
