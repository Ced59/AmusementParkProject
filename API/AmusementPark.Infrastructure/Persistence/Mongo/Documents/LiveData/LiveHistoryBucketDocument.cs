using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LiveHistoryBucketDocument : MongoDocumentBase
{
    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("target")]
    public LiveTargetReferenceDocument Target { get; set; } = new LiveTargetReferenceDocument();

    [BsonElement("bucketStartUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime BucketStartUtc { get; set; }

    [BsonElement("bucketEndUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime BucketEndUtc { get; set; }

    [BsonElement("bucketDurationMilliseconds")]
    public long BucketDurationMilliseconds { get; set; }

    [BsonElement("usagePolicyVersion")]
    public string UsagePolicyVersion { get; set; } = string.Empty;

    [BsonElement("expiresAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("samples")]
    public List<LiveHistoryBucketSampleDocument> Samples { get; set; } =
        new List<LiveHistoryBucketSampleDocument>();

    [BsonElement("isTruncated")]
    public bool IsTruncated { get; set; }
}
