using System.ComponentModel.DataAnnotations;
using System.Security.Principal;

namespace WebAPI_JWT_Auth.Data.Repositoty
{
    public class User
    {
        [Key]
        public Guid UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public byte[]? PasswordHash { get; set; }
        public byte[]? PasswordSalt { get; set; }
        public int LoginAttempt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenCreatedAt { get; set; }
        public DateTime TokenExpires { get; set; }
        public string VerificationToken { get; set; } = string.Empty;
        public DateTime VerifiedAt { get; set; }
        public string PasswordResetToken { get; set; } = string.Empty;
        public DateTime ResetTokenExpires { get; set; }
        public DateTime UserCreatedAt { get; set; }
        public DateTime UserModifiedAt { get; set; }
        public int StateID { get; set; }
        public int UserProfileID { get; set; }

    }
}
