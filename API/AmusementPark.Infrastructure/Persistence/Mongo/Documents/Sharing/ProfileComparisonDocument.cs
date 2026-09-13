using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class ProfileComparisonDocument : MongoDocumentBase
{
    [BsonElement("invitationId")]
    public string InvitationId { get; set; } = string.Empty;

    [BsonElement("shareToken")]
    public string ShareToken { get; set; } = string.Empty;

    [BsonElement("creatorUserId")]
    public string CreatorUserId { get; set; } = string.Empty;

    [BsonElement("acceptorUserId")]
    public string AcceptorUserId { get; set; } = string.Empty;

    [BsonElement("creatorPassportPublicationId")]
    public string CreatorPassportPublicationId { get; set; } = string.Empty;

    [BsonElement("creatorPassportPublicationVersion")]
    public long CreatorPassportPublicationVersion { get; set; }

    [BsonElement("acceptorPassportPublicationId")]
    public string AcceptorPassportPublicationId { get; set; } = string.Empty;

    [BsonElement("acceptorPassportPublicationVersion")]
    public long AcceptorPassportPublicationVersion { get; set; }

    [BsonElement("calculation")]
    public ProfileComparisonCalculationDocument Calculation { get; set; } = new();

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ProfileComparisonStatus Status { get; set; }

    [BsonElement("revokedByUserId")]
    [BsonIgnoreIfNull]
    public string? RevokedByUserId { get; set; }

    [BsonElement("revokedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? RevokedAtUtc { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("isModerationSuspended")]
    public bool IsModerationSuspended { get; set; }
}
