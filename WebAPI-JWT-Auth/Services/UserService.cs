using Microsoft.AspNetCore.Mvc;
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

        public async Task<User> GetMyName()
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return null;
            }
            var userName = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);

            return await _dataContext.TbUser.FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<User> UserRegister(UserViewModel userViewModel)
        {
            var foundUser = await _dataContext.TbUser.FirstOrDefaultAsync(u => u.UserName == userViewModel.UserName);
            if (foundUser != null)
            {
                return null;
            }

            CreatePasswordHash(userViewModel.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User()
            {
                UserName = userViewModel.UserName,
                UserEmail = userViewModel.UserEmail,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                UserCreatedAt = DateTime.Now,
                UserModifiedAt = DateTime.Now,
                VerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64)),
                VerificationTokenCreatedAt = DateTime.Now,
                VerificationTokenExpires = DateTime.Now.AddHours(8)
            };

            _dataContext.TbUser.Add(user);
            await _dataContext.SaveChangesAsync();

            return user;
        }

        public async Task<User> UserEdit(UserViewModel userViewModel)
        {
            var foundUser = await _dataContext.TbUser.FindAsync(userViewModel.UserID);
            if (foundUser == null)
            {
                return null;
            }

            CreatePasswordHash(userViewModel.Password, out byte[] passwordHash, out byte[] passwordSalt);

            foundUser.UserName = userViewModel.UserName;
            foundUser.PasswordHash = passwordHash;
            foundUser.PasswordSalt = passwordSalt;
            foundUser.UserModifiedAt = DateTime.Now;

            await _dataContext.SaveChangesAsync();

            return foundUser;

        }

        public async Task<UserViewModel> UserLogin(UserViewModel userViewModel)
        {
            var foundUser = await _dataContext.TbUser.FirstOrDefaultAsync(u => u.UserName == userViewModel.UserName);
            if (foundUser == null)
            {
                return null;
            }

            if (foundUser.LoginAttempt > 3 || foundUser.UserVerifiedAt == null)
            {
                return null;
            }

            if (!VerifyPasswordHash(userViewModel.Password, foundUser.PasswordHash, foundUser.PasswordSalt))
            {
                return null;
            }

            string token = await CreateToken(foundUser);
            userViewModel.Token = token;
            userViewModel.UserID = foundUser.UserID;

            var refreshToken = await RefreshToken(userViewModel);

            if (refreshToken == null)
            {
                return null;
            }

            userViewModel.RefreshToken = refreshToken.Token;

            return userViewModel;
        }

        public async Task<UserViewModel> UserVerify(string verfiyToken)
        {
            var foundUser = await _dataContext.TbUser.FirstOrDefaultAsync(
                u => u.VerificationToken.Equals(verfiyToken)
                && u.UserVerifiedAt == null
                && u.VerificationTokenExpires > DateTime.Now);
            if (foundUser == null)
            {
                return null;
            }

            foundUser.UserVerifiedAt = DateTime.Now;
            foundUser.VerificationToken = string.Empty;
            foundUser.UserModifiedAt = DateTime.Now;

            await _dataContext.SaveChangesAsync();

            var userViewModel = new UserViewModel()
            {
                UserID = foundUser.UserID,
                UserName = foundUser.UserName
            };

            return userViewModel;
        }

        public async Task<User> ForgotPassword(UserViewModel userViewModel)
        {
            var foundUser = await _dataContext.TbUser.FirstOrDefaultAsync(u => u.UserName.Equals(userViewModel.UserName));
            if (foundUser == null)
            {
                return null;
            }

            foundUser.UserModifiedAt = DateTime.Now;
            foundUser.PasswordResetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
            foundUser.ResetTokenExpires = DateTime.Now.AddHours(8);
            foundUser.UserModifiedAt = DateTime.Now;

            await _dataContext.SaveChangesAsync();

            return foundUser;
        }

        public async Task<User> ResetPassword(UserViewModel userViewModel)
        {
            var foundUser = await _dataContext.TbUser.FirstOrDefaultAsync(u => u.PasswordResetToken == userViewModel.PasswordResetToken);
            if (foundUser == null || foundUser.ResetTokenExpires < DateTime.Now)
            {
                return null;
            }

            CreatePasswordHash(userViewModel.Password, out byte[] passwordHash, out byte[] passwordSalt);

            foundUser.PasswordHash = passwordHash;
            foundUser.PasswordSalt = passwordSalt;
            foundUser.PasswordResetToken = string.Empty;
            foundUser.UserModifiedAt = DateTime.Now;

            await _dataContext.SaveChangesAsync();

            return foundUser;
        }
        public async Task<object> UserDelete(Guid userID)
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

        public async Task<User> UserByID(Guid userID)
        {
            var foundUser = await _dataContext.TbUser.FindAsync(userID);
            if (foundUser == null)
            {
                return null;
            }

            return foundUser;
        }

        public async Task<User> UserByUserName(string userName)
        {
            var foundUser = await _dataContext.TbUser.FirstOrDefaultAsync(u => u.UserName == userName);
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
        public async Task<string> CreateToken(User user)
        {
            var foundUser = await _dataContext.TbUser.FindAsync(user.UserID);
            if (foundUser == null)
            {
                return null;
            }

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var expires = DateTime.Now.AddHours(8);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expires,
                signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            foundUser.Token = jwt;
            foundUser.TokenCreatedAt = DateTime.Now;
            foundUser.TokenExpires = expires;
            foundUser.UserModifiedAt = DateTime.Now;

            await _dataContext.SaveChangesAsync();

            return jwt;
        }
        public async Task<string> CreateVerificationToken(User user)
        {
            var foundUser = await _dataContext.TbUser.FindAsync(user.UserID);
            if (foundUser == null)
            {
                return null;
            }

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var expires = DateTime.Now.AddHours(8);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expires,
                signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            foundUser.VerificationToken = jwt;
            foundUser.VerificationTokenCreatedAt = DateTime.Now;
            foundUser.VerificationTokenExpires = expires;
            foundUser.UserModifiedAt = DateTime.Now;

            await _dataContext.SaveChangesAsync();

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

        public async Task<RefreshToken> RefreshToken(UserViewModel userViewModel)
        {
            var foundUser = await _dataContext.TbUser.FindAsync(userViewModel.UserID);
            if (foundUser == null)
            {
                return null;
            }

            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.Now.AddHours(3),
                Created = DateTime.Now
            };

            foundUser.RefreshToken = refreshToken.Token;
            foundUser.RefreshTokenCreatedAt = refreshToken.Created;
            foundUser.RefreshTokenExpires = refreshToken.Expires;
            foundUser.UserModifiedAt = DateTime.Now;

            await _dataContext.SaveChangesAsync();

            return refreshToken;
        }

        public void AppendCookie(HttpResponse response, string refreshToken, DateTime? refreshTokenExpires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = refreshTokenExpires
            };

            response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
