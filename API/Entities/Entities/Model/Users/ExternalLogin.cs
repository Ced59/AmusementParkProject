using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Users
{
    public class ExternalLogin
    {
        [BsonRepresentation(BsonType.String)]
        public ExternalLoginProvider Provider { get; set; }

        public string ProviderUserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; }

        public string? DisplayName { get; set; }

        public string? GivenName { get; set; }

        public string? FamilyName { get; set; }

        public string? PictureUrl { get; set; }

        public string? HostedDomain { get; set; }

        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    }
}
