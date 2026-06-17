using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialShare;

[BsonIgnoreExtraElements]
public sealed class SocialShareEventDocument : MongoDocumentBase
{
    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("targetType")]
    public string TargetType { get; set; } = string.Empty;

    [BsonElement("targetId")]
    [BsonIgnoreIfNull]
    public string? TargetId { get; set; }

    [BsonElement("targetTitle")]
    [BsonIgnoreIfNull]
    public string? TargetTitle { get; set; }

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("languageCode")]
    [BsonIgnoreIfNull]
    public string? LanguageCode { get; set; }

    [BsonElement("channel")]
    public string Channel { get; set; } = string.Empty;

    [BsonElement("visitorKind")]
    public string VisitorKind { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonIgnoreIfNull]
    public string? UserId { get; set; }
}
