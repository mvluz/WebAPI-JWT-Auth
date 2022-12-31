namespace WebAPI_JWT_Auth.Data.ViewModels
{
    public class UserViewModel
    {
        public Guid UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime? TokenCreatedAt { get; set; }
        public DateTime? TokenExpires { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime? RefreshTokenCreatedAt { get; set; }
        public DateTime? RefreshTokenExpires { get; set; }
        public string VerificationToken { get; set; } = string.Empty;
        public DateTime? VerificationTokenCreatedAt { get; set; }
        public DateTime? VerificationTokenExpires { get; set; }
        public DateTime? UserVerifiedAt { get; set; }
        public string PasswordResetToken { get; set; } = string.Empty;
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? UserCreatedAt { get; set; }
        public DateTime? UserModifiedAt { get; set; }

    }
}
