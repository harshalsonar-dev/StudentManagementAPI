using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StudentManagementSystem.DTOs.Auth;
using StudentManagementSystem.DTOs.Common;
using StudentManagementSystem.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StudentManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _config = config;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return BadRequest(ApiResponse<string>.FailureResult("Validation failed.", errors));
            }

            var email = request.Email.Trim();
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<string>.FailureResult($"A user with email '{email}' already exists."));
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(ApiResponse<string>.FailureResult("User registration failed.", errors));
            }

            // Assign default role
            var targetRole = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role.Trim();
            if (await _roleManager.RoleExistsAsync(targetRole))
            {
                await _userManager.AddToRoleAsync(user, targetRole);
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Student");
            }

            _logger.LogInformation("New user registered: {Email}", email);
            return Ok(ApiResponse<string>.SuccessResult("User registered successfully."));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return BadRequest(ApiResponse<AuthResponseDto>.FailureResult("Validation failed.", errors));
            }

            var email = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Unauthorized(ApiResponse<AuthResponseDto>.FailureResult("Invalid email or password."));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account locked out for user: {Email}", email);
                return StatusCode(StatusCodes.Status423Locked,
                    ApiResponse<AuthResponseDto>.FailureResult("Account is temporarily locked due to multiple failed login attempts. Please try again later."));
            }

            if (!result.Succeeded)
            {
                return Unauthorized(ApiResponse<AuthResponseDto>.FailureResult("Invalid email or password."));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiration) = GenerateJwtToken(user, roles);

            var authResponse = new AuthResponseDto
            {
                Message = "Login successful.",
                Token = token,
                Expiration = expiration,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Roles = roles
            };

            _logger.LogInformation("User logged in successfully: {Email}", email);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Login successful."));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<string>.FailureResult("Validation failed."));
            }

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null)
            {
                return NotFound(ApiResponse<string>.FailureResult($"User with email '{request.Email}' not found."));
            }

            var roleExists = await _roleManager.RoleExistsAsync(request.Role.Trim());
            if (!roleExists)
            {
                return BadRequest(ApiResponse<string>.FailureResult($"Role '{request.Role}' does not exist."));
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains(request.Role.Trim()))
            {
                var result = await _userManager.AddToRoleAsync(user, request.Role.Trim());
                if (!result.Succeeded)
                {
                    return BadRequest(ApiResponse<string>.FailureResult("Failed to assign role.", result.Errors.Select(e => e.Description)));
                }
            }

            return Ok(ApiResponse<string>.SuccessResult($"Role '{request.Role}' assigned successfully to {request.Email}."));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("roles")]
        public IActionResult GetRoles()
        {
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return Ok(ApiResponse<IEnumerable<string?>>.SuccessResult(roles, "Roles retrieved successfully."));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<string>.FailureResult("User identifier claim missing."));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.FailureResult("User not found."));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var profile = new
            {
                user.Id,
                user.Email,
                user.UserName,
                Roles = roles
            };

            return Ok(ApiResponse<object>.SuccessResult(profile, "Profile retrieved successfully."));
        }

        private (string Token, DateTime Expiration) GenerateJwtToken(IdentityUser user, IList<string> roles)
        {
            var key = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key is not configured.");

            var issuer = _config["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is not configured.");

            var audience = _config["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT audience is not configured.");

            var durationInMinutes = _config.GetValue<int>("Jwt:DurationInMinutes", 120);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email!),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddMinutes(durationInMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiration,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
        }
    }
}
