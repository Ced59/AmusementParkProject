using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LiveOperationalControlDocument : MongoDocumentBase
{
    [BsonElement("controlId")]
    public string ControlId { get; set; } = string.Empty;

    [BsonElement("version")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("scopeType")]
    [BsonRepresentation(BsonType.String)]
    public LiveOperationalScopeType ScopeType { get; set; }

    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("externalEntityId")]
    [BsonIgnoreIfNull]
    public string? ExternalEntityId { get; set; }

    [BsonElement("internalParkId")]
    [BsonIgnoreIfNull]
    public string? InternalParkId { get; set; }

    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public LiveTargetType? TargetType { get; set; }

    [BsonElement("internalTargetId")]
    [BsonIgnoreIfNull]
    public string? InternalTargetId { get; set; }

    [BsonElement("collectionEnabled")]
    public bool CollectionEnabled { get; set; }

    [BsonElement("publicReadEnabled")]
    public bool PublicReadEnabled { get; set; }

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
