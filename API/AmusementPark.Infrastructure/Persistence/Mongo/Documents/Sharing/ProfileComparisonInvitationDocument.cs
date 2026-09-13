using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class ProfileComparisonInvitationDocument : MongoDocumentBase
{
    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    [BsonElement("creatorUserId")]
    public string CreatorUserId { get; set; } = string.Empty;

    [BsonElement("creatorPassportPublicationId")]
    public string CreatorPassportPublicationId { get; set; } = string.Empty;

    [BsonElement("creatorPassportPublicationVersion")]
    public long CreatorPassportPublicationVersion { get; set; }

    [BsonElement("categories")]
    [BsonRepresentation(BsonType.String)]
    public List<ProfileComparisonCategory> Categories { get; set; } = new();

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ProfileComparisonInvitationStatus Status { get; set; }

    [BsonElement("acceptorUserId")]
    [BsonIgnoreIfNull]
    public string? AcceptorUserId { get; set; }

    [BsonElement("acceptorPassportPublicationId")]
    [BsonIgnoreIfNull]
    public string? AcceptorPassportPublicationId { get; set; }

    [BsonElement("acceptorPassportPublicationVersion")]
    [BsonIgnoreIfNull]
    public long? AcceptorPassportPublicationVersion { get; set; }

    [BsonElement("comparisonId")]
    [BsonIgnoreIfNull]
    public string? ComparisonId { get; set; }

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("acceptedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? AcceptedAtUtc { get; set; }

    [BsonElement("purgeAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? PurgeAtUtc { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
