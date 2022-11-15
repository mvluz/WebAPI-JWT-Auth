using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace WebAPI_JWT_Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly DataContext _dataContext;

        public AuthController(IConfiguration configuration, IUserService userService, DataContext dataContext)
        {
            _configuration = configuration;
            _userService = userService;
            _dataContext = dataContext;
        }

        [HttpGet, Authorize]
        public ActionResult<string> GetMe()
        {
            var userName = _userService.GetMyName();

            return Ok(userName);
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDTO userRequest)
        {
            var foundUser = await _dataContext.Users.FirstOrDefaultAsync(u => u.UserName == userRequest.UserName);
            if (foundUser != null)
            {
                return BadRequest("User name already exists.");
            }

            CreatePasswordHash(userRequest.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User();
            user.UserName = userRequest.UserName;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            _dataContext.Users.Add(user);
            await _dataContext.SaveChangesAsync();

            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDTO userRequest)
        {
            var foundUser = await _dataContext.Users.FirstOrDefaultAsync(u => u.UserName == userRequest.UserName);
            if (foundUser == null)
            {
                return BadRequest("User not found.");
            }

            if (!VerifyPasswordHash(userRequest.Password, foundUser.PasswordHash, foundUser.PasswordSalt))
            {
                return BadRequest("Wrong password.");
            }

            string token = CreateToken(foundUser);

            return Ok(token);
        }

        [HttpGet("userbyid/{id}")]
        public async Task<ActionResult<User>> UserById(int id)
        {
            var foundUser = await _dataContext.Users.FindAsync(id);
            if (foundUser == null)
            {
                return BadRequest("User not found.");
            }
            return Ok(foundUser);
        }

        [HttpPut("useredit")]
        public async Task<ActionResult<User>> UserEdit(UserDTO user)
        {
            var foundUser = await _dataContext.Users.FindAsync(user.Id);
            if (foundUser == null)
            {
                return BadRequest("User not found.");
            }

            CreatePasswordHash(user.Password, out byte[] passwordHash, out byte[] passwordSalt);

            foundUser.UserName = user.UserName;
            foundUser.PasswordHash = passwordHash;
            foundUser.PasswordSalt = passwordSalt;

            await _dataContext.SaveChangesAsync();

            return Ok(foundUser);
        }

        [HttpDelete("userdelete/{id}")]
        public async Task<ActionResult<object>> UserDelete(int id)
        {
            var foundUser = await _dataContext.Users.FindAsync(id);
            if (foundUser == null)
            {
                return BadRequest("User not found.");
            }

            _dataContext.Users.Remove(foundUser);
            await _dataContext.SaveChangesAsync();

            return Ok(new { msg = "User Deleted.", user = foundUser });
        }

        [HttpGet("userslist")]
        public async Task<ActionResult<List<User>>> UsersList()
        {
            return Ok(await _dataContext.Users.ToListAsync());
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
