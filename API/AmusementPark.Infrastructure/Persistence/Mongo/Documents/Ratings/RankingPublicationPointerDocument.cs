using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

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
