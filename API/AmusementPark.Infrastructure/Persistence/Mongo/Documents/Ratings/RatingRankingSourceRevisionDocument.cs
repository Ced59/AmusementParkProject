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

    [BsonElement("unavailableMethodologyVersion")]
    [BsonIgnoreIfNull]
    public string? UnavailableMethodologyVersion { get; set; }

    [BsonElement("highestUnavailableSourceRevision")]
    [BsonIgnoreIfNull]
    public long? HighestUnavailableSourceRevision { get; set; }
}
