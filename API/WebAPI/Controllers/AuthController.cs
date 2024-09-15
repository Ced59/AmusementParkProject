using Dtos.Users.Login;
using Dtos.Users.RefreshToken;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Services.Interfaces;
using System.Net.Http.Headers;
using Dtos.Users.Creating;
using Entities.Model.Errors;
using Entities.Model.Users;
using Microsoft.IdentityModel.Tokens;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;
using OneOf.Types;

namespace WebAPI.Controllers;

[ApiController]
[SwaggerOrder(1)]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly ISocialAuthService _socialAuthService;

    public AuthController(IUsersService usersService, ISocialAuthService socialAuthService)
    {
        _usersService = usersService;
        _socialAuthService = socialAuthService;
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


    [HttpGet("facebook")]
    public IActionResult AuthenticateFacebook()
    {
        var authenticationProperties = new AuthenticationProperties { RedirectUri = Url.Action("FacebookResponse") };
        return Challenge(authenticationProperties, FacebookDefaults.AuthenticationScheme);
    }

    [HttpPost("google-response")]
    public async Task<IActionResult> GoogleResponse([FromBody] CodeModel model)
    {
        var provider = "Google";

        try
        {
            if (!model.Code.IsNullOrEmpty())
            {
                var accessToken = await _socialAuthService.ExchangeGoogleCodeForToken(provider, model.Code);
                var userInfo = await _socialAuthService.GetGoogleUserInfo(provider, accessToken);

                var userLogged = await _usersService.GetUserByEmailAsync(userInfo.Email);

                if (userLogged.IsT0)
                {
                    var userToLog = await _usersService.LoginExternalAsync(userLogged.AsT0.Email);
                    return ApiResponseHandler.HandleResponse(userToLog);
                }

                if (userLogged.AsT1.StatusCode == ErrorCodes.UserNotExists.StatusCode)
                {
                    var userToCreate = new UserSocialCreate
                    {
                        Email = userInfo.Email,
                        AvatarUrl = userInfo.Picture,
                        FirstName = userInfo.GivenName,
                        LastName = userInfo.FamilyName
                    };

                    var created = await _usersService.CreateUserByInfosAsync(userToCreate);
                    return ApiResponseHandler.HandleResponse(created);

                }
                else
                {
                    return ApiResponseHandler.HandleResponse(ErrorCodes.LoginFailed);
                }
            }


            return BadRequest($"An error occurred.");
            // Vous pouvez renvoyer les informations de l'utilisateur ou une autre réponse appropriée
        }
        catch (Exception ex)
        {
            return BadRequest($"An error occurred: {ex.Message}");
        }
    }

    public class CodeModel
    {
        public string? Code { get; set; }
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