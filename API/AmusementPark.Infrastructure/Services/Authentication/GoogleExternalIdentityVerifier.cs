using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Authentication;
using Google.Apis.Auth;

namespace AmusementPark.Infrastructure.Services.Authentication;

/// <summary>
/// Vérificateur d'identités externes Google.
/// </summary>
public sealed class GoogleExternalIdentityVerifier : IExternalIdentityVerifier
{
    private readonly GoogleOAuthSettings googleOAuthSettings;

    public GoogleExternalIdentityVerifier(GoogleOAuthSettings googleOAuthSettings)
    {
        this.googleOAuthSettings = googleOAuthSettings;
    }

    public bool Supports(ExternalLoginProvider provider)
    {
        return provider == ExternalLoginProvider.Google && !string.IsNullOrWhiteSpace(this.googleOAuthSettings.ClientId);
    }

    public async Task<VerifiedExternalIdentity?> VerifyAsync(ExternalLoginProvider provider, string token, string? nonce, CancellationToken cancellationToken = default)
    {
        if (!this.Supports(provider) || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        GoogleJsonWebSignature.ValidationSettings validationSettings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { this.googleOAuthSettings.ClientId },
        };

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(token, validationSettings);
        }
        catch (InvalidJwtException)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(nonce) && !string.Equals(payload.Nonce, nonce, StringComparison.Ordinal))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
        {
            return null;
        }

        bool isEmailAuthoritative = payload.EmailVerified &&
                                    (payload.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)
                                     || payload.Email.EndsWith("@googlemail.com", StringComparison.OrdinalIgnoreCase)
                                     || !string.IsNullOrWhiteSpace(payload.HostedDomain));

        return new VerifiedExternalIdentity
        {
            Provider = ExternalLoginProvider.Google,
            ProviderUserId = payload.Subject,
            Email = payload.Email,
            IsEmailVerified = payload.EmailVerified,
            IsEmailAuthoritative = isEmailAuthoritative,
            DisplayName = payload.Name,
            GivenName = payload.GivenName,
            FamilyName = payload.FamilyName,
            PictureUrl = payload.Picture,
            HostedDomain = payload.HostedDomain,
        };
    }
}
