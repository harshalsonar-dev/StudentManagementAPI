using FluentValidation;
using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Interfaces;

namespace StudentManagement.Application.Features.Auth.Commands.RefreshToken
{
    public record RefreshTokenCommand(RefreshTokenRequestDto Dto) : IRequest<ApiResponse<AuthResponseDto>>;

    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.Dto.AccessToken)
                .NotEmpty().WithMessage("Access token is required.");

            RuleFor(x => x.Dto.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.");
        }
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponseDto>>
    {
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public async Task<ApiResponse<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var tokenResponse = await _tokenService.RefreshTokenAsync(
                request.Dto.AccessToken,
                request.Dto.RefreshToken,
                cancellationToken);

            return ApiResponse<AuthResponseDto>.SuccessResult(tokenResponse, "Token refreshed successfully.");
        }
    }
}
