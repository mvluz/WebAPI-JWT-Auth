using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebAPI_JWT_Auth.Data.Repositoty;
using WebAPI_JWT_Auth.Data.ViewModels;

namespace WebAPI_JWT_Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly DataContext _dataContext;
        private readonly string _genericErrorMessage = "The Action Finished with Error.";

        public AuthController(IUserService userService, DataContext dataContext)
        {
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
        public async Task<ActionResult<User>> Register(UserViewModel userViewModel)
        {
            var createdUser = await _userService.UserRegister(userViewModel);
            if (createdUser == null)
            {
                return BadRequest(_genericErrorMessage);
            }

            return Ok(createdUser);
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserViewModel>> Login(UserViewModel userViewModel)
        {
            var userViewModelVerified = await _userService.UserLogin(userViewModel);
            if (userViewModelVerified.Token == string.Empty)
            {
                return BadRequest(_genericErrorMessage);
            }

            var refreshToken = await _userService.RefreshToken(userViewModelVerified);

            if (refreshToken == null)
            {
                return BadRequest(_genericErrorMessage);
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = refreshToken.Expires
            };

            Response.Cookies.Append("refreshToken", refreshToken.Token, cookieOptions);

            var userReturn = new UserViewModel()
            {
                UserID = userViewModelVerified.UserID,
                UserName = userViewModelVerified.UserName,
                Token = userViewModelVerified.Token
            };

            return Ok(userReturn);
        }

        [HttpPost("resfreshtoken"), Authorize]
        public async Task<ActionResult<UserViewModel>> RefreshToken(UserViewModel userViewModel)
        {
            var refreshTokenRequest = Request.Cookies["refreshToken"];

            var foundUser = await _userService.UserByName(userViewModel.UserName);
            
            if (!foundUser.RefreshToken.Equals(refreshTokenRequest))
            {
                return BadRequest(_genericErrorMessage);
            }
            else if(foundUser.TokenExpires < DateTime.Now)
            {
                return BadRequest(_genericErrorMessage);
            }

            userViewModel.Token = _userService.CreateToken(foundUser);
            userViewModel.UserID = foundUser.UserID;

            var refreshToken = await _userService.RefreshToken(userViewModel);

            if (refreshToken == null)
            {
                return BadRequest(_genericErrorMessage);
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = refreshToken.Expires
            };

            Response.Cookies.Append("refreshToken", refreshToken.Token, cookieOptions);

            return Ok(userViewModel);
        }

        [HttpGet("userbyid/{id}"), Authorize]
        public async Task<ActionResult<User>> UserByID(Guid userID)
        {
            var foundUser = await _userService.UserByID(userID);
            if (foundUser == null)
            {
                return BadRequest(_genericErrorMessage);
            }
            return Ok(foundUser);
        }

        [HttpPut("useredit"), Authorize]
        public async Task<ActionResult<User>> UserEdit(UserViewModel user)
        {
            var foundUser = await _userService.UserEdit(user);

            return Ok(foundUser);
        }

        [HttpDelete("userdelete/{id}"), Authorize]
        public async Task<ActionResult<object>> UserDelete(Guid userID)
        {
            var deletedUser = await _dataContext.TbUser.FindAsync(userID);
            if (deletedUser == null)
            {
                return BadRequest(_genericErrorMessage);
            }

            return Ok(deletedUser);
        }

        [HttpGet("userslist"), Authorize]
        public async Task<ActionResult<List<User>>> UsersList()
        {
            return Ok(await _userService.UsersList());
        }

    }
}
