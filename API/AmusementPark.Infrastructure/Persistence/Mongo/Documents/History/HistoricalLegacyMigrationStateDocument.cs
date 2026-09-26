using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoricalLegacyMigrationStateDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("startedAtUtc")]
    public DateTime StartedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("leaseOwner")]
    [BsonIgnoreIfNull]
    public string? LeaseOwner { get; set; }

    [BsonElement("leaseExpiresAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LeaseExpiresAtUtc { get; set; }

    [BsonElement("sourceCount")]
    public long SourceCount { get; set; }

    [BsonElement("sourceDigest")]
    public string SourceDigest { get; set; } = string.Empty;

    [BsonElement("backupCount")]
    public long BackupCount { get; set; }

    [BsonElement("narrativeCount")]
    public long NarrativeCount { get; set; }

    [BsonElement("factCount")]
    public long FactCount { get; set; }

    [BsonElement("sourceReferenceCount")]
    public long SourceReferenceCount { get; set; }

    [BsonElement("blockedCount")]
    public long BlockedCount { get; set; }

    [BsonElement("warningCount")]
    public long WarningCount { get; set; }

    [BsonElement("lastError")]
    [BsonIgnoreIfNull]
    public string? LastError { get; set; }
}
