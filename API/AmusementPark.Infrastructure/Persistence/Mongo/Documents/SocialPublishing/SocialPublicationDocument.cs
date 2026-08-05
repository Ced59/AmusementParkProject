using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialPublishing;

[BsonIgnoreExtraElements]
public sealed class SocialPublicationDocument : MongoDocumentBase
{
    [BsonElement("network")]
    public string Network { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("trigger")]
    public string Trigger { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("sourceEntityType")]
    [BsonIgnoreIfNull]
    public string? SourceEntityType { get; set; }

    [BsonElement("sourceEntityId")]
    [BsonIgnoreIfNull]
    public string? SourceEntityId { get; set; }

    [BsonElement("requestedByUserId")]
    [BsonIgnoreIfNull]
    public string? RequestedByUserId { get; set; }

    [BsonElement("deduplicationKey")]
    [BsonIgnoreIfNull]
    public string? DeduplicationKey { get; set; }

    [BsonElement("requestedAtUtc")]
    public DateTime RequestedAtUtc { get; set; }

    [BsonElement("attemptedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? AttemptedAtUtc { get; set; }

    [BsonElement("publishedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? PublishedAtUtc { get; set; }

    [BsonElement("deletedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? DeletedAtUtc { get; set; }

    [BsonElement("lastSynchronizedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastSynchronizedAtUtc { get; set; }

    [BsonElement("externalPostId")]
    [BsonIgnoreIfNull]
    public string? ExternalPostId { get; set; }

    [BsonElement("externalPostUrl")]
    [BsonIgnoreIfNull]
    public string? ExternalPostUrl { get; set; }

    [BsonElement("failureCode")]
    [BsonIgnoreIfNull]
    public string? FailureCode { get; set; }

    [BsonElement("failureMessage")]
    [BsonIgnoreIfNull]
    public string? FailureMessage { get; set; }
}
