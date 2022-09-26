using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public static List<User> users = new List<User>();
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public AuthController(IConfiguration configuration, IUserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        [HttpGet, Authorize]
        public ActionResult<string> GetMe()
        {
            var userName = _userService.GetMyName();

            return Ok(userName);
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDTO request)
        {
            var foundUser = users.Find(u => u.UserName == request.UserName);
            if (foundUser != null)
            {
                return BadRequest("User name already exists.");
            }

            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var user = new User();
            var indexId = users.Count() - 1;
            user.Id = indexId < 0 ? 1 : users[indexId].Id + 1;
            user.UserName = request.UserName;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            users.Add(user);

            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDTO request)
        {
            var foundUser = users.Find(u => u.UserName == request.UserName);
            if (foundUser == null)
            {
                return BadRequest("User not found.");
            }

            if (!VerifyPasswordHash(request.Password, foundUser.PasswordHash, foundUser.PasswordSalt))
            {
                return BadRequest("Wrong password.");
            }

            string token = CreateToken(foundUser);

            return Ok(token);
        }

        [HttpGet("userbyid/{id}")]
        public async Task<ActionResult<User>> UserById(int id)
        {
            var foundUser = users.Find(u => u.Id == id);
            if (foundUser == null)
            {
                return BadRequest("User not found.");
            }
            return Ok(foundUser);
        }

        [HttpPut("useredit")]
        public async Task<ActionResult<User>> UserEdit(UserDTO user)
        {
            var foundUser = users.Find(u => u.Id == user.Id);
            if (foundUser == null)
            {
                return BadRequest("User not found.");
            }

            CreatePasswordHash(user.Password, out byte[] passwordHash, out byte[] passwordSalt);

            foundUser.UserName = user.UserName;
            foundUser.PasswordHash = passwordHash;
            foundUser.PasswordSalt = passwordSalt;

            return Ok(foundUser);
        }

        [HttpDelete("userdelete/{id}")]
        public async Task<ActionResult<object>> UserDelete(int id)
        {
            var foundUser = users.Find(u => u.Id == id);
            if (foundUser == null)
            {
                return BadRequest("User not found.");
            }

            users.Remove(foundUser);
            return Ok(new { msg = "User Deleted.", user = foundUser });
        }

        [HttpGet("userslist")]
        public async Task<ActionResult<List<User>>> UsersList()
        {
            return Ok(users);
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
