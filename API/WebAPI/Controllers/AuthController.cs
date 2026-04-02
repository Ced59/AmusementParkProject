using Dtos.Users.ExternalLogin;
using Dtos.Users.Login;
using Dtos.Users.RefreshToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Entities.Model.Errors;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;
using OneOf;

namespace WebAPI.Controllers
{
    [ApiController]
    [SwaggerOrder(2)]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsersService usersService;
        private readonly IExternalAuthenticationService externalAuthenticationService;

        public AuthController(IUsersService usersService, IExternalAuthenticationService externalAuthenticationService)
        {
            this.usersService = usersService;
            this.externalAuthenticationService = externalAuthenticationService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto userLoginDto)
        {
            OneOf<UserLoggedDto, ErrorCodes.ErrorDetail> userLogged = await usersService.LoginAsync(userLoginDto);
            return ApiResponseHandler.HandleResponse(userLogged);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDto token)
        {
            OneOf<RefreshTokenResponseDto, ErrorCodes.ErrorDetail> tokenRefreshed = await usersService.RefreshTokenAsync(token);
            return ApiResponseHandler.HandleResponse(tokenRefreshed);
        }

        [HttpPost("external/{provider}")]
        public async Task<IActionResult> ExternalLoginAsync(
            string provider,
            [FromBody] ExternalLoginRequestDto request,
            CancellationToken cancellationToken)
        {
            OneOf<UserLoggedDto, ErrorCodes.ErrorDetail> authenticationResult = await externalAuthenticationService.AuthenticateAsync(
                provider,
                request.Token,
                request.Nonce,
                cancellationToken);

            return ApiResponseHandler.HandleResponse(authenticationResult);
        }

        [HttpGet("facebook")]
        public IActionResult AuthenticateFacebook()
        {
            AuthenticationProperties authenticationProperties = new()
            {
                RedirectUri = Url.Action("FacebookResponse")
            };

            return Challenge(authenticationProperties, FacebookDefaults.AuthenticationScheme);
        }

        [HttpGet("facebook-response")]
        public async Task<IActionResult> FacebookResponse()
        {
            AuthenticateResult result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return BadRequest("Error from Facebook authentication");
            }

            return Ok();
        }
    }
}
