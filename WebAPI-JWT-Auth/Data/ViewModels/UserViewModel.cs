namespace WebAPI_JWT_Auth.Data.ViewModels
{
    public class UserViewModel
    {
        public Guid? Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
