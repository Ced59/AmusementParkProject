using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Contracts.Users;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature Auth migrée en phase 10.
/// </summary>
[ApiController]
[Route("[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ICommandHandler<LoginCommand, ApplicationResult<AuthenticatedUserResult>> loginCommandHandler;
    private readonly ICommandHandler<RefreshTokenCommand, ApplicationResult<AuthenticatedUserResult>> refreshTokenCommandHandler;
    private readonly ICommandHandler<ProvisionExternalUserCommand, ApplicationResult<AuthenticatedUserResult>> provisionExternalUserCommandHandler;
    private readonly IAuthenticationSchemeProvider authenticationSchemeProvider;

    public AuthController(
        ICommandHandler<LoginCommand, ApplicationResult<AuthenticatedUserResult>> loginCommandHandler,
        ICommandHandler<RefreshTokenCommand, ApplicationResult<AuthenticatedUserResult>> refreshTokenCommandHandler,
        ICommandHandler<ProvisionExternalUserCommand, ApplicationResult<AuthenticatedUserResult>> provisionExternalUserCommandHandler,
        IAuthenticationSchemeProvider authenticationSchemeProvider)
    {
        this.loginCommandHandler = loginCommandHandler;
        this.refreshTokenCommandHandler = refreshTokenCommandHandler;
        this.provisionExternalUserCommandHandler = provisionExternalUserCommandHandler;
        this.authenticationSchemeProvider = authenticationSchemeProvider;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(UserLoggedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto userLoginDto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<AuthenticatedUserResult> result = await this.loginCommandHandler.HandleAsync(
            new LoginCommand(userLoginDto.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new UserLoggedDto
        {
            Token = result.Value.AccessToken,
        });
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDto token, CancellationToken cancellationToken = default)
    {
        ApplicationResult<AuthenticatedUserResult> result = await this.refreshTokenCommandHandler.HandleAsync(
            new RefreshTokenCommand(token.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new RefreshTokenResponseDto
        {
            RefreshToken = result.Value.RefreshToken,
        });
    }

    [HttpPost("external/{provider}")]
    [ProducesResponseType(typeof(UserLoggedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExternalLoginAsync([FromRoute] string provider, [FromBody] ExternalLoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse(provider, true, out ExternalLoginProvider parsedProvider))
        {
            return CreateLegacyError(StatusCodes.Status400BadRequest, "External authentication provider is not supported.");
        }

        ProvisionExternalUserRequest applicationRequest = new ProvisionExternalUserRequest
        {
            Provider = parsedProvider,
            Token = request.Token,
            Nonce = request.Nonce,
        };

        ApplicationResult<AuthenticatedUserResult> result = await this.provisionExternalUserCommandHandler.HandleAsync(
            new ProvisionExternalUserCommand(applicationRequest),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new UserLoggedDto
        {
            Token = result.Value.AccessToken,
        });
    }

    [HttpGet("facebook")]
    public async Task<IActionResult> AuthenticateFacebook()
    {
        AuthenticationScheme? scheme = await this.authenticationSchemeProvider.GetSchemeAsync("Facebook");
        if (scheme is null)
        {
            return this.BadRequest("Facebook authentication is not configured.");
        }

        AuthenticationProperties authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = this.Url.Action(nameof(FacebookResponse)) ?? "/auth/facebook-response",
        };

        return this.Challenge(authenticationProperties, "Facebook");
    }

    [HttpGet("facebook-response")]
    public async Task<IActionResult> FacebookResponse()
    {
        AuthenticationScheme? scheme = await this.authenticationSchemeProvider.GetSchemeAsync("Facebook");
        if (scheme is null)
        {
            return this.BadRequest("Facebook authentication is not configured.");
        }

        AuthenticateResult result = await this.HttpContext.AuthenticateAsync("Facebook");
        if (!result.Succeeded)
        {
            return this.BadRequest("Error from Facebook authentication");
        }

        return this.Ok();
    }

    private static ObjectResult CreateLegacyError(int statusCode, string message)
    {
        return new ObjectResult(new
        {
            StatusCode = statusCode,
            Message = message,
        })
        {
            StatusCode = statusCode,
        };
    }
}
