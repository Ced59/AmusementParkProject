using Dtos.Users.Login;
using Dtos.Users.RefreshToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Entities.Model.Errors;
using Entities.Model.Users;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;
using Dtos.Users.Updating;
using Dtos.Users.UserGet;
using OneOf;

namespace WebAPI.Controllers
{
    [ApiController]
    [SwaggerOrder(2)]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsersService usersService;
        private readonly ISocialAuthService socialAuthService;

        public AuthController(IUsersService usersService, ISocialAuthService socialAuthService)
    {
        this.usersService = usersService;
        this.socialAuthService = socialAuthService;
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


        [HttpGet("facebook")]
        public IActionResult AuthenticateFacebook()
    {
        AuthenticationProperties authenticationProperties = new() { RedirectUri = Url.Action("FacebookResponse") };
        return Challenge(authenticationProperties, FacebookDefaults.AuthenticationScheme);
    }

        [HttpPost("google-response")]
        public async Task<IActionResult> GoogleResponse([FromBody] CodeModel model)
    {
        string provider = "Google";

        try
        {
            if (!string.IsNullOrEmpty(model.Code))
            {
                string accessToken = await socialAuthService.ExchangeGoogleCodeForToken(provider, model.Code);
                UserGoogleInfos userInfo = await socialAuthService.GetGoogleUserInfo(provider, accessToken);

                OneOf<UserGettedDto, ErrorCodes.ErrorDetail> userLogged = await usersService.GetUserByEmailAsync(userInfo.Email);

                if (userLogged.IsT0)
                {
                    UserGettedDto? existingUser = userLogged.AsT0;

                    // Vérifier si l'utilisateur n'a pas encore d'avatar
                    if (string.IsNullOrEmpty(existingUser.AvatarUrl) && !string.IsNullOrEmpty(userInfo.Picture))
                    {
                        string avatarPath = await usersService.DownloadAndSaveUserAvatar(userInfo.Picture, existingUser.Id);
                        if (!string.IsNullOrEmpty(avatarPath))
                        {
                            // Mettre à jour l'utilisateur avec le nouvel avatar
                            UserUpdateDto updateDto = new()
                            {
                                Email = existingUser.Email,
                                FirstName = existingUser.FirstName,
                                LastName = existingUser.LastName,
                                PreferredLanguage = existingUser.PreferredLanguage,
                                AvatarUrl = avatarPath
                            };
                            await usersService.UpdateUserAsync(existingUser.Id, updateDto);
                        }
                    }

                    OneOf<UserLoggedDto, ErrorCodes.ErrorDetail> userToLog = await usersService.LoginExternalAsync(existingUser.Email);
                    return ApiResponseHandler.HandleResponse(userToLog);
                }

                if (userLogged.AsT1.StatusCode == ErrorCodes.UserNotExists.StatusCode)
                {
                    string avatarPath = !string.IsNullOrEmpty(userInfo.Picture)
                        ? await usersService.DownloadAndSaveUserAvatar(userInfo.Picture, Guid.NewGuid().ToString())
                        : "";

                    UserSocialCreate userToCreate = new()
                    {
                        Email = userInfo.Email,
                        AvatarUrl = avatarPath,
                        FirstName = userInfo.GivenName,
                        LastName = userInfo.FamilyName
                    };

                    OneOf<UserLoggedDto, ErrorCodes.ErrorDetail> created = await usersService.CreateUserByInfosAsync(userToCreate);
                    return ApiResponseHandler.HandleResponse(created);
                }

                return ApiResponseHandler.HandleResponse(ErrorCodes.LoginFailed);
            }

            return BadRequest("An error occurred.");
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
        AuthenticateResult result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            return BadRequest("Error from Facebook authentication");
        }

        // Traiter l'authentification réussie ici
        return Ok();
    }

    }
}