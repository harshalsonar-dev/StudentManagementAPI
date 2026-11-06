using FluentValidation;
using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Enums;
using StudentManagement.Domain.Exceptions;

namespace StudentManagement.Application.Features.Auth.Commands.Register
{
    public record RegisterCommand(RegisterRequestDto Dto) : IRequest<ApiResponse<string>>;

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Dto.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches(@"[\!\?\*\@\#\$\%\^\&\+\=]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.Dto.Role)
                .Must(role => string.IsNullOrEmpty(role) || AppRoles.All.Contains(role))
                .WithMessage($"Role must be one of: {string.Join(", ", AppRoles.All)}");
        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<string>>
    {
        private readonly IIdentityService _identityService;

        public RegisterCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<ApiResponse<string>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var role = string.IsNullOrWhiteSpace(request.Dto.Role) ? AppRoles.Student : request.Dto.Role.Trim();

            var (succeeded, _, errors) = await _identityService.CreateUserAsync(
                request.Dto.Email.Trim(),
                request.Dto.Password,
                request.Dto.FullName?.Trim() ?? string.Empty,
                role,
                cancellationToken);

            if (!succeeded)
            {
                var errorList = errors.ToList();
                if (errorList.Any(e => e.Contains("already", StringComparison.OrdinalIgnoreCase) || e.Contains("taken", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ConflictException($"User with email '{request.Dto.Email}' already exists.");
                }

                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    { "Identity", errorList.ToArray() }
                });
            }

            return ApiResponse<string>.SuccessResult("User registered successfully.");
        }
    }
}
