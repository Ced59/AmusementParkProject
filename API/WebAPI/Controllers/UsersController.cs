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
            var userCreated = await _usersService!.CreateUserAsync(user);

            return ApiResponseHandler.HandleResponse(userCreated);
        }

        [HttpGet]
        [Route("by-email")]
        public async Task<IActionResult> GetUserByEmailAsync([FromQuery] string email)
        {
            var user = await _usersService.GetUserByEmailAsync(email);

            return ApiResponseHandler.HandleResponse(user);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserByIdAsync(string id)
        {
            var user = await _usersService.GetUserByIdAsync(id);
            return ApiResponseHandler.HandleResponse(user);
        }
    }

}
