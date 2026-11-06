namespace StudentManagementSystem.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();
    }
}

