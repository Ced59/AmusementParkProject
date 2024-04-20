using Entities.Model.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using WebAPI.ResponseHandlers;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpPost]
        //[Authorize(Roles = "Admin,RH")]
        public async Task<IActionResult> CreateUserAsync([FromBody] UserCreate user)
        {
            var userCreated = await _usersService!.CreateUser(user);

            return ApiResponseHandler.HandleResponse(userCreated);
        }
    }

}
