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
        public string GetMyName();

        public Task<User> UserEdit(UserViewModel userViewModel);

        public Task<User> UserRegister(UserViewModel userViewModel);

        public Task<string> UserLogin(UserViewModel userViewModel);

        public Task<object> UserDelete(Guid userID);

        public Task<User> UserByID(Guid userID);

        public Task<List<User>> UsersList();

    }

}
