using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login()
    {
        try
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "admin")
            };

            var keyString = _config["Jwt:Key"];

            // 🔥 DEBUG CHECK
            if (string.IsNullOrEmpty(keyString))
            {
                return StatusCode(500, "JWT Key is NULL. Check appsettings.json");
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(keyString));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new { token = tokenString });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}