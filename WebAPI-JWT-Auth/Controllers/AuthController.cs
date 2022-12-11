using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<ActionResult<string>> Login(UserViewModel userViewModel)
        {
            var token = await _userService.UserLogin(userViewModel);
            if (token == null)
            {
                return BadRequest(_genericErrorMessage);
            }

            return Ok(token);
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
