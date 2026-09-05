using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class SharePublicationDocument : MongoDocumentBase
{
    [BsonElement("ownerUserId")]
    public string OwnerUserId { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public SharePublicationType Type { get; set; }

    [BsonElement("sourceScopeKey")]
    public string SourceScopeKey { get; set; } = string.Empty;

    [BsonElement("shareToken")]
    [BsonIgnoreIfNull]
    public string? ShareToken { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public SharePublicationStatus Status { get; set; }

    [BsonElement("visibility")]
    [BsonRepresentation(BsonType.String)]
    public ShareVisibility Visibility { get; set; }

    [BsonElement("contentPolicy")]
    public ShareContentPolicyDocument ContentPolicy { get; set; } = new ShareContentPolicyDocument();

    [BsonElement("sourceVersion")]
    public long SourceVersion { get; set; }

    [BsonElement("publicationVersion")]
    public long PublicationVersion { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("publishedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? PublishedAtUtc { get; set; }

    [BsonElement("revokedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? RevokedAtUtc { get; set; }
}
