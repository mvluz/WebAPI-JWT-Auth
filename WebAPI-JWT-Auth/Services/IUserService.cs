using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using WebAPI_JWT_Auth.Data.Repositoty;
using WebAPI_JWT_Auth.Data.ViewModels;

namespace WebAPI_JWT_Auth.Services
{
    public interface IUserService
    {
        public Task<User> GetMyName();

        public Task<User> UserEdit(UserViewModel userViewModel);

        public Task<User> UserRegister(UserViewModel userViewModel);

        public Task<UserViewModel> UserLogin(UserViewModel userViewModel);

        public Task<UserViewModel> UserVerify(string verfiyToken);

        public Task<object> UserDelete(Guid userID);

        public Task<User> UserByID(Guid userID);

        public Task<User> UserByName(string userName);

        public Task<List<User>> UsersList();

        public Task<RefreshToken> RefreshToken(UserViewModel userViewModel);

        public Task<string> CreateToken(User user, string type);

        public void AppendCookie(HttpResponse response, string refreshToken, DateTime? refreshTokenExpires);

    }

}
