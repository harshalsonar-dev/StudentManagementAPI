using FluentValidation;
using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Exceptions;

namespace StudentManagement.Application.Features.Auth.Commands.Login
{
    public record LoginCommand(LoginRequestDto Dto) : IRequest<ApiResponse<AuthResponseDto>>;

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Dto.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
    {
        private readonly IIdentityService _identityService;
        private readonly ITokenService _tokenService;

        public LoginCommandHandler(IIdentityService identityService, ITokenService tokenService)
        {
            _identityService = identityService;
            _tokenService = tokenService;
        }

        public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var (succeeded, isLockedOut, userId, userName, roles) = await _identityService.AuthenticateAsync(
                request.Dto.Email.Trim(),
                request.Dto.Password,
                cancellationToken);

            if (isLockedOut)
            {
                throw new UnauthorizedAppException("Account is locked out due to multiple failed login attempts. Please try again after 5 minutes.");
            }

            if (!succeeded)
            {
                throw new UnauthorizedAppException("Invalid email or password.");
            }

            var tokenResponse = await _tokenService.GenerateTokensAsync(userId, request.Dto.Email, userName, roles, cancellationToken);
            return ApiResponse<AuthResponseDto>.SuccessResult(tokenResponse, "Login successful.");
        }
    }
}
