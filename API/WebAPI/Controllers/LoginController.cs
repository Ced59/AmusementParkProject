using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Dtos.Users; 
using System.Threading.Tasks;
using Dtos.Users.Login;
using OneOf;
using static Entities.Model.Errors.ErrorCodes;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;

namespace WebAPI.Controllers
{
    [ApiController]
    [SwaggerOrder(1)]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public LoginController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto userLoginDto)
        {
            var userLogged = new UserLoggedDto();
            return ApiResponseHandler.HandleResponse(OneOf<UserLoggedDto, ErrorDetail>.FromT0(userLogged));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] string token)
        {
            
            return Ok();
        }

        [HttpGet("auth/google")]
        public IActionResult AuthenticateGoogle()
        {
            var authenticationProperties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(authenticationProperties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("auth/facebook")]
        public IActionResult AuthenticateFacebook()
        {
            var authenticationProperties = new AuthenticationProperties { RedirectUri = Url.Action("FacebookResponse") };
            return Challenge(authenticationProperties, FacebookDefaults.AuthenticationScheme);
        }

        [HttpGet("auth/google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return BadRequest("Error from Google authentication"); 

            // Traiter l'authentification réussie ici
            return Ok();
        }

        [HttpGet("auth/facebook-response")]
        public async Task<IActionResult> FacebookResponse()
        {
            var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return BadRequest("Error from Facebook authentication"); 

            // Traiter l'authentification réussie ici
            return Ok();
        }


    }
}