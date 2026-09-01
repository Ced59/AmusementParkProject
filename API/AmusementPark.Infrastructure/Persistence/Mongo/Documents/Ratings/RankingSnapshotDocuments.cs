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

[BsonIgnoreExtraElements]
public sealed class RankingSnapshotChunkDocument : MongoDocumentBase
{
    [BsonElement("scopeKey")]
    public string ScopeKey { get; set; } = string.Empty;

    [BsonElement("snapshotId")]
    public string SnapshotId { get; set; } = string.Empty;

    [BsonElement("chunkIndex")]
    public int ChunkIndex { get; set; }

    [BsonElement("firstRank")]
    public int FirstRank { get; set; }

    [BsonElement("lastRank")]
    public int LastRank { get; set; }

    [BsonElement("firstPosition")]
    public int FirstPosition { get; set; }

    [BsonElement("lastPosition")]
    public int LastPosition { get; set; }

    [BsonElement("entryCount")]
    public int EntryCount { get; set; }

    [BsonElement("buildAttempt")]
    public int BuildAttempt { get; set; }

    [BsonElement("checksum")]
    public string Checksum { get; set; } = string.Empty;

    [BsonElement("entries")]
    public List<RankingSnapshotEntryDocument> Entries { get; set; } = new List<RankingSnapshotEntryDocument>();
}

public sealed class RankingSnapshotEntryDocument
{
    [BsonElement("position")]
    public int Position { get; set; }

    [BsonElement("rank")]
    public int Rank { get; set; }

    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    public RatingTargetType TargetType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("parkItemCategory")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public ParkItemCategory? ParkItemCategory { get; set; }

    [BsonElement("score")]
    public double Score { get; set; }

    [BsonElement("evidenceLevel")]
    [BsonRepresentation(BsonType.String)]
    public RankingEvidenceLevel EvidenceLevel { get; set; }

    [BsonElement("uniqueContributorCount")]
    public int UniqueContributorCount { get; set; }

    [BsonElement("ratingObservationCount")]
    public int RatingObservationCount { get; set; }

    [BsonElement("directParkContributorCount")]
    [BsonIgnoreIfNull]
    public int? DirectParkContributorCount { get; set; }

    [BsonElement("itemContributorCount")]
    [BsonIgnoreIfNull]
    public int? ItemContributorCount { get; set; }

    [BsonElement("eligibleItemCount")]
    [BsonIgnoreIfNull]
    public int? EligibleItemCount { get; set; }

    [BsonElement("eligibleCategoryCount")]
    [BsonIgnoreIfNull]
    public int? EligibleCategoryCount { get; set; }

    [BsonElement("isSingleCategoryParkException")]
    [BsonIgnoreIfNull]
    public bool? IsSingleCategoryParkException { get; set; }

    [BsonElement("nextContributorThreshold")]
    [BsonIgnoreIfNull]
    public int? NextContributorThreshold { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class RankingPublicationPointerDocument : MongoDocumentBase
{
    [BsonElement("scopeKey")]
    public string ScopeKey { get; set; } = string.Empty;

    [BsonElement("currentSnapshotId")]
    public string CurrentSnapshotId { get; set; } = string.Empty;

    [BsonElement("currentSnapshotPublishedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CurrentSnapshotPublishedAtUtc { get; set; }

    [BsonElement("previousSnapshotId")]
    [BsonIgnoreIfNull]
    public string? PreviousSnapshotId { get; set; }

    [BsonElement("previousSnapshotPublishedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? PreviousSnapshotPublishedAtUtc { get; set; }

    [BsonElement("methodologyVersion")]
    public string MethodologyVersion { get; set; } = string.Empty;

    [BsonElement("sourceRevision")]
    public long SourceRevision { get; set; }

    [BsonElement("highestPublishedSourceRevision")]
    public long HighestPublishedSourceRevision { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
