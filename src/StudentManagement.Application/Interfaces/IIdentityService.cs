namespace StudentManagement.Application.Interfaces
{
    public interface IIdentityService
    {
        Task<(bool Succeeded, string UserId, IEnumerable<string> Errors)> CreateUserAsync(
            string email,
            string password,
            string fullName,
            string role,
            CancellationToken cancellationToken = default);

        Task<(bool Succeeded, bool IsLockedOut, string UserId, string UserName, IList<string> Roles)> AuthenticateAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default);

        Task<bool> AssignRoleAsync(
            string email,
            string role,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetAllRolesAsync(
            CancellationToken cancellationToken = default);
    }
}
