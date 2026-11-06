using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Exceptions;

namespace StudentManagement.Application.Features.Auth.Commands.RevokeToken
{
    public record RevokeTokenCommand(string RefreshToken) : IRequest<ApiResponse<bool>>;

    public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, ApiResponse<bool>>
    {
        private readonly ITokenService _tokenService;

        public RevokeTokenCommandHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public async Task<ApiResponse<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            var revoked = await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (!revoked)
            {
                throw new NotFoundException("Active refresh token not found or already revoked.");
            }

            return ApiResponse<bool>.SuccessResult(true, "Refresh token revoked successfully.");
        }
    }
}
