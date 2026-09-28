using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LiveLatestObservationDocument : MongoDocumentBase
{
    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("target")]
    public LiveTargetReferenceDocument Target { get; set; } = new LiveTargetReferenceDocument();

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public LiveOperationalStatus Status { get; set; }

    [BsonElement("queues")]
    public List<LiveQueueObservationDocument> Queues { get; set; } = new List<LiveQueueObservationDocument>();

    [BsonElement("provenance")]
    public LiveObservationProvenanceDocument Provenance { get; set; } =
        new LiveObservationProvenanceDocument();

    [BsonElement("freshnessPolicy")]
    public LiveFreshnessPolicyDocument FreshnessPolicy { get; set; } =
        new LiveFreshnessPolicyDocument();

    [BsonElement("expiresAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ExpiresAtUtc { get; set; }

    [BsonElement("payloadSha256")]
    [BsonIgnoreIfNull]
    public string? PayloadSha256 { get; set; }

    [BsonElement("hasStatusQueueConflict")]
    public bool HasStatusQueueConflict { get; set; }
}
