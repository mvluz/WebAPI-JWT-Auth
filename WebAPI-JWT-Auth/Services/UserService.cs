using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using WebAPI_JWT_Auth.Data;
using WebAPI_JWT_Auth.Data.Repositoty;
using WebAPI_JWT_Auth.Data.ViewModels;

namespace WebAPI_JWT_Auth.Services
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration; 
        private readonly DataContext _dataContext;
        public UserService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, DataContext dataContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _dataContext = dataContext;
        }

        public string GetMyName()
        {
            var result = string.Empty;
            if(_httpContextAccessor.HttpContext != null)
            {
                result = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            }
            return result;
        }

        public async Task<User> UserRegister (UserViewModel userViewModel)
        {
            var foundUser = await _dataContext.TbUser.FirstOrDefaultAsync(u => u.UserName == userViewModel.UserName);
            if (foundUser != null)
            {
                return null;
            }

            CreatePasswordHash(userViewModel.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User();
            user.UserName = userViewModel.UserName;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            _dataContext.TbUser.Add(user);
            await _dataContext.SaveChangesAsync();

            return user;
        }
 
        public async Task<User> UserEdit(UserViewModel userViewModel)
        {
            var foundUser = await _dataContext.TbUser.FindAsync(userViewModel.Id);
            if (foundUser == null)
            {
                return null;
            }

            CreatePasswordHash(userViewModel.Password, out byte[] passwordHash, out byte[] passwordSalt);

            foundUser.UserName = userViewModel.UserName;
            foundUser.PasswordHash = passwordHash;
            foundUser.PasswordSalt = passwordSalt;

            await _dataContext.SaveChangesAsync();

            return foundUser;

        }

        public async Task<string> UserLogin (UserViewModel userViewModel)
        {
            var foundUser = await _dataContext.TbUser.FirstOrDefaultAsync(u => u.UserName == userViewModel.UserName);
            if (foundUser == null)
            {
                return null;
            }

            if (!VerifyPasswordHash(userViewModel.Password, foundUser.PasswordHash, foundUser.PasswordSalt))
            {
                return null;
            }

            string token = CreateToken(foundUser);

            return token;

        }

        public async Task<object>UserDelete (Guid userID)
        {
            var foundUser = await _dataContext.TbUser.FindAsync(userID);
            if (foundUser == null)
            {
                return null;
            }

            _dataContext.TbUser.Remove(foundUser);
            await _dataContext.SaveChangesAsync();

            return new { msg = "User Deleted.", user = foundUser };
        }
        public async Task<User> UserByID (Guid userID)
        {
            var foundUser = await _dataContext.TbUser.FindAsync(userID);
            if (foundUser == null)
            {
                return null;
            }

            return foundUser;
        }

        public async Task<List<User>> UsersList()
        {
            return await _dataContext.TbUser.ToListAsync();
        }
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }

    }
}
