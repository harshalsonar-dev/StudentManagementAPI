namespace StudentManagement.Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty; // To associate with the JWT JTI
        public DateTime ExpiresOnUtc { get; set; }
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedOnUtc { get; set; }
        public string? ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresOnUtc;
        public bool IsRevoked => RevokedOnUtc != null;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
