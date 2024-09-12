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
        string state = GenerateRandomString();
        HttpContext.Session.SetString("oauth_state", state);
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleResponse"),
            Items = { { "state", state } }
        };
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
        {
            return BadRequest("Error from Google authentication");
        }

        // Récupération de l'état de la session
        var expectedState = HttpContext.Session.GetString("oauth_state");
        if (result.Properties.Items["state"] != expectedState)
        {
            return BadRequest("Invalid state parameter");
        }

        // Traitement supplémentaire
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



    private string GenerateRandomString(int length = 32)
    {
        using (var randomNumberGenerator = new System.Security.Cryptography.RNGCryptoServiceProvider())
        {
            var randomBytes = new byte[length];
            randomNumberGenerator.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }

}