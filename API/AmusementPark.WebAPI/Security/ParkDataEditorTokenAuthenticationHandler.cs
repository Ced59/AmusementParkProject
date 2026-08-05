using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Queries;
using AmusementPark.Application.Features.ParkDataEditorTokens.Results;
using AmusementPark.Core.Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AmusementPark.WebAPI.Security;

public sealed class ParkDataEditorTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IQueryHandler<AuthenticateParkDataEditorTokenQuery, ApplicationResult<ParkDataEditorTokenAuthenticationResult>> authenticationHandler;

    public ParkDataEditorTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IQueryHandler<AuthenticateParkDataEditorTokenQuery, ApplicationResult<ParkDataEditorTokenAuthenticationResult>> authenticationHandler)
        : base(options, logger, encoder)
    {
        this.authenticationHandler = authenticationHandler;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string authorizationHeader = this.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out AuthenticationHeaderValue? header)
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter)
            || !header.Parameter.StartsWith(ParkDataEditorAuthenticationDefaults.TokenPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        ApplicationResult<ParkDataEditorTokenAuthenticationResult> result = await this.authenticationHandler.HandleAsync(
            new AuthenticateParkDataEditorTokenQuery(header.Parameter),
            this.Context.RequestAborted);
        if (!result.IsSuccess || result.Value is null)
        {
            return AuthenticateResult.Fail("Invalid park data editor token.");
        }

        ParkDataEditorTokenAuthenticationResult authentication = result.Value;
        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, authentication.User.Id),
            new Claim(JwtRegisteredClaimNames.Sub, authentication.User.Id),
            new Claim(ClaimTypes.Name, authentication.User.Email ?? authentication.User.Id),
            new Claim(ClaimTypes.Email, authentication.User.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Email, authentication.User.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, "PARK_DATA_EDITOR"),
            new Claim(
                ParkDataEditorAuthenticationDefaults.AuthenticationMethodClaim,
                ParkDataEditorAuthenticationDefaults.AuthenticationMethod),
            new Claim(ParkDataEditorAuthenticationDefaults.TokenIdClaim, authentication.Token.Id),
            new Claim(ParkDataEditorAuthenticationDefaults.TokenLabelClaim, authentication.Token.Label),
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, this.Scheme.Name);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        AuthenticationTicket ticket = new AuthenticationTicket(principal, this.Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
