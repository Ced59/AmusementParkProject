using Entities.Model.Users;
using Google.Apis.Auth;
using Services.Interfaces.Authentication;
using Services.Interfaces.Settings;
using Services.Models.Authentication;

namespace Services.Implementations.Authentication
{
    public class GoogleExternalIdentityProviderService : IExternalIdentityProviderService
    {
        private readonly IGoogleOAuthSettings googleOAuthSettings;

        public GoogleExternalIdentityProviderService(IGoogleOAuthSettings googleOAuthSettings)
        {
            this.googleOAuthSettings = googleOAuthSettings;
        }

        public ExternalLoginProvider Provider => ExternalLoginProvider.Google;

        public async Task<VerifiedExternalIdentity?> VerifyAsync(string token, string? nonce, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(googleOAuthSettings.ClientId))
            {
                return null;
            }

            GoogleJsonWebSignature.ValidationSettings validationSettings = new()
            {
                Audience = new[] { googleOAuthSettings.ClientId }
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
                HostedDomain = payload.HostedDomain
            };
        }
    }
}
