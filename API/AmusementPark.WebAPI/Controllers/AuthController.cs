using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Contracts.Users;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

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
    private readonly ICommandHandler<RevokeRefreshTokenCommand, ApplicationResult> revokeRefreshTokenCommandHandler;
    private readonly IAuthenticationSchemeProvider authenticationSchemeProvider;
    private readonly RefreshTokenCookieService refreshTokenCookieService;
    private readonly CorsSettings corsSettings;

    public AuthController(
        ICommandHandler<LoginCommand, ApplicationResult<AuthenticatedUserResult>> loginCommandHandler,
        ICommandHandler<RefreshTokenCommand, ApplicationResult<AuthenticatedUserResult>> refreshTokenCommandHandler,
        ICommandHandler<ProvisionExternalUserCommand, ApplicationResult<AuthenticatedUserResult>> provisionExternalUserCommandHandler,
        ICommandHandler<RevokeRefreshTokenCommand, ApplicationResult> revokeRefreshTokenCommandHandler,
        IAuthenticationSchemeProvider authenticationSchemeProvider,
        RefreshTokenCookieService refreshTokenCookieService,
        CorsSettings corsSettings)
    {
        this.loginCommandHandler = loginCommandHandler;
        this.refreshTokenCommandHandler = refreshTokenCommandHandler;
        this.provisionExternalUserCommandHandler = provisionExternalUserCommandHandler;
        this.revokeRefreshTokenCommandHandler = revokeRefreshTokenCommandHandler;
        this.authenticationSchemeProvider = authenticationSchemeProvider;
        this.refreshTokenCookieService = refreshTokenCookieService;
        this.corsSettings = corsSettings;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthLogin)]
    [ProducesResponseType(typeof(UserLoggedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto userLoginDto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<AuthenticatedUserResult> result = await this.loginCommandHandler.HandleAsync(
            new LoginCommand(userLoginDto.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            this.refreshTokenCookieService.DeleteRefreshTokenCookie(this.Response);
            return this.ToActionResult(result);
        }

        this.ApplyRefreshTokenCookie(result.Value);

        return this.Ok(new UserLoggedDto
        {
            Token = result.Value.AccessToken,
        });
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthRefresh)]
    [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDto? token, CancellationToken cancellationToken = default)
    {
        if (!this.IsAllowedCredentialOrigin())
        {
            this.refreshTokenCookieService.DeleteRefreshTokenCookie(this.Response);
            return this.ToProblemDetailsResult(StatusCodes.Status403Forbidden, "Origin is not allowed for credentialed authentication requests.", "auth.origin-not-allowed");
        }

        string? refreshTokenFromCookie = this.refreshTokenCookieService.GetRefreshToken(this.Request);
        string refreshToken = !string.IsNullOrWhiteSpace(refreshTokenFromCookie)
            ? refreshTokenFromCookie
            : token?.RefreshToken ?? string.Empty;

        ApplicationResult<AuthenticatedUserResult> result = await this.refreshTokenCommandHandler.HandleAsync(
            new RefreshTokenCommand(new RefreshTokenRequest
            {
                RefreshToken = refreshToken,
            }),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            this.refreshTokenCookieService.DeleteRefreshTokenCookie(this.Response);
            return this.ToActionResult(result);
        }

        this.ApplyRefreshTokenCookie(result.Value);

        return this.Ok(new RefreshTokenResponseDto
        {
            AccessToken = result.Value.AccessToken,
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        if (!this.IsAllowedCredentialOrigin())
        {
            this.refreshTokenCookieService.DeleteRefreshTokenCookie(this.Response);
            return this.ToProblemDetailsResult(StatusCodes.Status403Forbidden, "Origin is not allowed for credentialed authentication requests.", "auth.origin-not-allowed");
        }

        string? refreshToken = this.refreshTokenCookieService.GetRefreshToken(this.Request);
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await this.revokeRefreshTokenCommandHandler.HandleAsync(
                new RevokeRefreshTokenCommand(refreshToken, "UserLogout"),
                cancellationToken);
        }

        this.refreshTokenCookieService.DeleteRefreshTokenCookie(this.Response);
        return this.NoContent();
    }

    [HttpPost("external/{provider}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.AuthExternalLogin)]
    [ProducesResponseType(typeof(UserLoggedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExternalLoginAsync([FromRoute] string provider, [FromBody] ExternalLoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse(provider, true, out ExternalLoginProvider parsedProvider))
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "External authentication provider is not supported.", "auth.external-provider-not-supported");
        }

        ProvisionExternalUserRequest applicationRequest = new ProvisionExternalUserRequest
        {
            Provider = parsedProvider,
            Token = request.Token,
            Nonce = request.Nonce,
            PreferredMeasurementSystem = request.PreferredMeasurementSystem,
        };

        ApplicationResult<AuthenticatedUserResult> result = await this.provisionExternalUserCommandHandler.HandleAsync(
            new ProvisionExternalUserCommand(applicationRequest),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            this.refreshTokenCookieService.DeleteRefreshTokenCookie(this.Response);
            return this.ToActionResult(result);
        }

        this.ApplyRefreshTokenCookie(result.Value);

        return this.Ok(new UserLoggedDto
        {
            Token = result.Value.AccessToken,
        });
    }

    [HttpGet("facebook")]
    [AllowAnonymous]
    public async Task<IActionResult> AuthenticateFacebook()
    {
        AuthenticationScheme? scheme = await this.authenticationSchemeProvider.GetSchemeAsync("Facebook");
        if (scheme is null)
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "Facebook authentication is not configured.", "auth.facebook-not-configured");
        }

        AuthenticationProperties authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = this.Url.Action(nameof(FacebookResponse)) ?? "/auth/facebook-response",
        };

        return this.Challenge(authenticationProperties, "Facebook");
    }

    [HttpGet("facebook-response")]
    [AllowAnonymous]
    public async Task<IActionResult> FacebookResponse()
    {
        AuthenticationScheme? scheme = await this.authenticationSchemeProvider.GetSchemeAsync("Facebook");
        if (scheme is null)
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "Facebook authentication is not configured.", "auth.facebook-not-configured");
        }

        AuthenticateResult result = await this.HttpContext.AuthenticateAsync("Facebook");
        if (!result.Succeeded)
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "Error from Facebook authentication.", "auth.facebook-error");
        }

        return this.Ok();
    }

    private void ApplyRefreshTokenCookie(AuthenticatedUserResult result)
    {
        if (string.IsNullOrWhiteSpace(result.RefreshToken))
        {
            this.refreshTokenCookieService.DeleteRefreshTokenCookie(this.Response);
            return;
        }

        this.refreshTokenCookieService.AppendRefreshTokenCookie(
            this.Response,
            result.RefreshToken,
            result.RefreshTokenExpiresAtUtc);
    }

    private bool IsAllowedCredentialOrigin()
    {
        string? origin = this.Request.Headers.Origin;
        if (string.IsNullOrWhiteSpace(origin))
        {
            return true;
        }

        string normalizedOrigin = origin.TrimEnd('/');
        string[] allowedOrigins = this.corsSettings.AllowedOrigins
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowedOrigins.Length == 0)
        {
            return true;
        }

        return allowedOrigins.Contains(normalizedOrigin, StringComparer.OrdinalIgnoreCase);
    }

}
