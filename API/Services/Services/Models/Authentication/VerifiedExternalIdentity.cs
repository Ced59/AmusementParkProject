using Entities.Model.Users;

namespace Services.Models.Authentication
{
    public class VerifiedExternalIdentity
    {
        public ExternalLoginProvider Provider { get; set; }

        public string ProviderUserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; }

        public bool IsEmailAuthoritative { get; set; }

        public string? DisplayName { get; set; }

        public string? GivenName { get; set; }

        public string? FamilyName { get; set; }

        public string? PictureUrl { get; set; }

        public string? HostedDomain { get; set; }
    }
}
