using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class NotificationEmailPreferenceDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; }

    [BsonElement("consentTextVersion")]
    [BsonIgnoreIfNull]
    public string? ConsentTextVersion { get; set; }

    [BsonElement("consentLocale")]
    [BsonIgnoreIfNull]
    public string? ConsentLocale { get; set; }

    [BsonElement("consentGrantedAt")]
    [BsonIgnoreIfNull]
    public DateTime? ConsentGrantedAt { get; set; }

    [BsonElement("revokedAt")]
    [BsonIgnoreIfNull]
    public DateTime? RevokedAt { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
