using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

[BsonIgnoreExtraElements]
public sealed class RatingRankingSourceRevisionDocument : MongoDocumentBase
{
    [BsonElement("scopeKey")]
    public string ScopeKey { get; set; } = string.Empty;

    [BsonElement("revision")]
    public long Revision { get; set; }

    [BsonElement("pendingMutationCount")]
    public int PendingMutationCount { get; set; }

    [BsonElement("mutationLeaseExpiresAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? MutationLeaseExpiresAtUtc { get; set; }

    [BsonElement("mutationLeases")]
    [BsonIgnoreIfDefault]
    public Dictionary<string, DateTime> MutationLeases { get; set; } = new();

    [BsonElement("mutationRecoveryTargets")]
    [BsonIgnoreIfDefault]
    public Dictionary<string, RatingRankingMutationRecoveryTargetDocument> MutationRecoveryTargets { get; set; } = new();

    [BsonElement("recoveredMutationTargets")]
    [BsonIgnoreIfDefault]
    public Dictionary<string, RatingRankingMutationRecoveryTargetDocument> RecoveredMutationTargets { get; set; } = new();

    [BsonElement("unavailableMethodologyVersion")]
    [BsonIgnoreIfNull]
    public string? UnavailableMethodologyVersion { get; set; }

    [BsonElement("highestUnavailableSourceRevision")]
    [BsonIgnoreIfNull]
    public long? HighestUnavailableSourceRevision { get; set; }

    [BsonElement("unavailableReasonCode")]
    [BsonIgnoreIfNull]
    public string? UnavailableReasonCode { get; set; }
}

public sealed class RatingRankingMutationRecoveryTargetDocument
{
    [BsonElement("targetType")]
    public string TargetType { get; set; } = string.Empty;

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("mutationToken")]
    public string MutationToken { get; set; } = string.Empty;
}
