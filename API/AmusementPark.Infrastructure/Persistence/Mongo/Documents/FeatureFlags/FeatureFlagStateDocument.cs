using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.FeatureFlags;

[BsonIgnoreExtraElements]
public sealed class FeatureFlagStateDocument : MongoDocumentBase
{
    [BsonElement("featureFlagId")]
    public string FeatureFlagId { get; set; } = string.Empty;

    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("environment")]
    public string Environment { get; set; } = string.Empty;

    [BsonElement("enabledOverride")]
    [BsonIgnoreIfNull]
    public bool? EnabledOverride { get; set; }

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("supersedesRevision")]
    [BsonIgnoreIfNull]
    public int? SupersedesRevision { get; set; }

    [BsonElement("changedByUserId")]
    public string ChangedByUserId { get; set; } = string.Empty;

    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;

    [BsonElement("recordedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime RecordedAtUtc { get; set; }
}
