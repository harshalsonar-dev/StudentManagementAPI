using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentManagement.API.Extensions;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.DTOs.Auth;
using StudentManagement.Application.Features.Auth.Commands.Login;
using StudentManagement.Application.Features.Auth.Commands.RefreshToken;
using StudentManagement.Application.Features.Auth.Commands.Register;
using StudentManagement.Application.Features.Auth.Commands.RevokeToken;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Enums;

namespace StudentManagement.API.Controllers
{
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public class AuthController : ApiControllerBase
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        /// <summary>
        /// Registers a new user account with default or specified role.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            var response = await Mediator.Send(new RegisterCommand(dto));
            return Ok(response);
        }

        /// <summary>
        /// Authenticates user and returns JWT Access Token with Refresh Token.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var response = await Mediator.Send(new LoginCommand(dto));
            return Ok(response);
        }

        /// <summary>
        /// Exchanges an expired access token and valid refresh token for a new token pair.
        /// </summary>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var response = await Mediator.Send(new RefreshTokenCommand(dto));
            return Ok(response);
        }

        /// <summary>
        /// Revokes an active refresh token.
        /// </summary>
        [HttpPost("revoke-token")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto dto)
        {
            var response = await Mediator.Send(new RevokeTokenCommand(dto.RefreshToken));
            return Ok(response);
        }

        /// <summary>
        /// Assigns a role to an existing user (Admin only).
        /// </summary>
        [Authorize(Roles = AppRoles.Admin)]
        [DisableRateLimiting]
        [HttpPost("assign-role")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
        {
            var succeeded = await _identityService.AssignRoleAsync(dto.Email, dto.Role);
            if (!succeeded)
            {
                return BadRequest(ApiResponse<string>.FailureResult($"Failed to assign role '{dto.Role}' to '{dto.Email}'."));
            }

            return Ok(ApiResponse<string>.SuccessResult($"Role '{dto.Role}' assigned successfully to '{dto.Email}'."));
        }

        /// <summary>
        /// Returns all available enterprise roles (Admin only).
        /// </summary>
        [Authorize(Roles = AppRoles.Admin)]
        [DisableRateLimiting]
        [HttpGet("roles")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _identityService.GetAllRolesAsync();
            return Ok(ApiResponse<IReadOnlyList<string>>.SuccessResult(roles, "Roles retrieved."));
        }

        /// <summary>
        /// Retrieves the profile and roles of the currently authenticated user.
        /// </summary>
        [Authorize]
        [DisableRateLimiting]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var profile = new
            {
                UserId = userId,
                Email = email,
                UserName = userName,
                Roles = roles
            };

            return Ok(ApiResponse<object>.SuccessResult(profile, "Current user profile retrieved."));
        }
    }
}
