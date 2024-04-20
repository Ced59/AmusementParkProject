using Entities.Model.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

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
        public IActionResult CreateUser([FromBody] UserCreate user)
        {
            var userCreated = _usersService.CreateUser(user);
            if (userCreated == null)
            {
                return BadRequest("Failed to create user");
            }
            return Ok(userCreated);
        }
    }

}
