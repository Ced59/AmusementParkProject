using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
internal sealed class SharePublicationMigrationStateDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("leaseOwner")]
    [BsonIgnoreIfNull]
    public string? LeaseOwner { get; set; }

    [BsonElement("leaseExpiresAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LeaseExpiresAtUtc { get; set; }

    [BsonElement("startedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? StartedAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("legacyCount")]
    public long LegacyCount { get; set; }

    [BsonElement("migratedCount")]
    public long MigratedCount { get; set; }

    [BsonElement("verifiedSampleCount")]
    public int VerifiedSampleCount { get; set; }

    [BsonElement("lastFailureCode")]
    [BsonIgnoreIfNull]
    public string? LastFailureCode { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
