using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Application.Interfaces;
using StudentManagement.Infrastructure.Identity;

namespace StudentManagement.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public async Task<(bool Succeeded, string UserId, IEnumerable<string> Errors)> CreateUserAsync(
            string email,
            string password,
            string fullName,
            string role,
            CancellationToken cancellationToken = default)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return (false, string.Empty, result.Errors.Select(e => e.Description));
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);
            return (true, user.Id, Enumerable.Empty<string>());
        }

        public async Task<(bool Succeeded, bool IsLockedOut, string UserId, string UserName, IList<string> Roles)> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return (false, false, string.Empty, string.Empty, Array.Empty<string>());
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                return (false, true, user.Id, user.UserName ?? user.Email!, Array.Empty<string>());
            }

            if (!result.Succeeded)
            {
                return (false, false, user.Id, user.UserName ?? user.Email!, Array.Empty<string>());
            }

            var roles = await _userManager.GetRolesAsync(user);
            return (true, false, user.Id, user.UserName ?? user.Email!, roles);
        }

        public async Task<bool> AssignRoleAsync(string email, string role, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(role))
            {
                var result = await _userManager.AddToRoleAsync(user, role);
                return result.Succeeded;
            }

            return true;
        }

        public async Task<IReadOnlyList<string>> GetAllRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _roleManager.Roles.Select(r => r.Name!).ToListAsync(cancellationToken);
        }
    }
}
