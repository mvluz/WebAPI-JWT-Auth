namespace WebAPI_JWT_Auth.Data.ViewModels
{
    public class UserViewModel
    {
        public Guid? UserID { get; set; } = Guid.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public string? Token { get; set; } = string.Empty;
    }
}
