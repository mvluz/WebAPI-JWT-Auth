using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
        public async Task<ActionResult<object>> GetMe()
        {
            var foundUser = await _userService.GetMyName();

            return Ok(new
            {
                foundUser.UserID,
                foundUser.UserName
            });
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
        public async Task<ActionResult<object>> Login(UserViewModel userViewModel)
        {
            var userViewModelVerified = await _userService.UserLogin(userViewModel);
            if (userViewModelVerified == null)
            {
                return BadRequest(_genericErrorMessage);
            }

            if (userViewModelVerified.Token.IsNullOrEmpty()
                && userViewModelVerified.RefreshToken.IsNullOrEmpty())
            {
                return BadRequest(_genericErrorMessage);
            }

            _userService.AppendCookie(Response, userViewModelVerified.RefreshToken, userViewModelVerified.RefreshTokenExpires);
            
            return Ok(new
            {
                userViewModelVerified.UserID,
                userViewModelVerified.UserName,
                userViewModelVerified.Token
            });
        }

        [HttpPost("verify")]
        public async Task<ActionResult<object>> Verify(string verifyToken)
        {
            var userViewModelVerified = await _userService.UserVerify(verifyToken);
            if (userViewModelVerified == null)
            {
                return BadRequest(_genericErrorMessage);
            }

            return Ok(new
            {
                userViewModelVerified.UserID,
                userViewModelVerified.UserName,
                message = $@"User {userViewModelVerified.UserName} Verified."
            });
        }

        [HttpPost("resfreshtoken"), Authorize]
        public async Task<ActionResult<UserViewModel>> RefreshToken(UserViewModel userViewModel)
        {
            var refreshTokenRequest = Request.Cookies["refreshToken"];

            var foundUser = await _userService.UserByID(userViewModel.UserID);
            if (foundUser == null)
            {
                return BadRequest(_genericErrorMessage);
            }

            if (!foundUser.RefreshToken.Equals(refreshTokenRequest)
                && foundUser.TokenExpires < DateTime.Now)
            {
                return BadRequest(_genericErrorMessage);
            }

            var refreshToken = await _userService.RefreshToken(userViewModel);
            if (refreshToken == null)
            {
                return BadRequest(_genericErrorMessage);
            }

            _userService.AppendCookie(Response, refreshToken.Token, refreshToken.Expires);

            return Ok(new
            {
                userViewModel.UserID,
                userViewModel.UserName
            });
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
