using Dtos.Users.Login;
using Dtos.Users.RefreshToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;

namespace WebAPI.Controllers;

[ApiController]
[SwaggerOrder(1)]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUsersService _usersService;

    public AuthController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto userLoginDto)
    {
        var userLogged = await _usersService.LoginAsync(userLoginDto);
        return ApiResponseHandler.HandleResponse(userLogged);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDto token)
    {
        var tokenRefreshed = await _usersService.RefreshTokenAsync(token);

        return ApiResponseHandler.HandleResponse(tokenRefreshed);
    }

    [HttpGet("google")]
    public IActionResult AuthenticateGoogle()
    {
        var authenticationProperties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
        return Challenge(authenticationProperties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("facebook")]
    public IActionResult AuthenticateFacebook()
    {
        var authenticationProperties = new AuthenticationProperties { RedirectUri = Url.Action("FacebookResponse") };
        return Challenge(authenticationProperties, FacebookDefaults.AuthenticationScheme);
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!result.Succeeded)
            return BadRequest("Error from Google authentication");

        // Traiter l'authentification réussie ici
        return Ok();
    }

    [HttpGet("facebook-response")]
    public async Task<IActionResult> FacebookResponse()
    {
        var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);
        if (!result.Succeeded)
            return BadRequest("Error from Facebook authentication");

        // Traiter l'authentification réussie ici
        return Ok();
    }
}