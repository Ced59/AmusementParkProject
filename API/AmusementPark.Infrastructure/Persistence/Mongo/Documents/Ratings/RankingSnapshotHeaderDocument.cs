using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

[BsonIgnoreExtraElements]
public sealed class RankingSnapshotHeaderDocument : MongoDocumentBase
{
    [BsonElement("scopeKey")]
    public string ScopeKey { get; set; } = string.Empty;

    [BsonElement("methodologyVersion")]
    public string MethodologyVersion { get; set; } = string.Empty;

    [BsonElement("sourceRevision")]
    public long SourceRevision { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public RankingSnapshotStatus Status { get; set; }

    [BsonElement("totalEntryCount")]
    public int TotalEntryCount { get; set; }

    [BsonElement("eligibleEntryCount")]
    public int EligibleEntryCount { get; set; }

    [BsonElement("chunkSize")]
    public int ChunkSize { get; set; }

    [BsonElement("chunkCount")]
    public int ChunkCount { get; set; }

    [BsonElement("checksum")]
    public string Checksum { get; set; } = string.Empty;

    [BsonElement("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; set; }

    [BsonElement("validatedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? ValidatedAtUtc { get; set; }

    [BsonElement("publishedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? PublishedAtUtc { get; set; }

    [BsonElement("failureCode")]
    [BsonIgnoreIfNull]
    public string? FailureCode { get; set; }

    [BsonElement("reconciledPointerVersion")]
    [BsonIgnoreIfNull]
    public long? ReconciledPointerVersion { get; set; }

    [BsonElement("buildAttempt")]
    public int BuildAttempt { get; set; }
}
