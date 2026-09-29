using AmusementPark.Core.Domain.LiveData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LiveHistoryBucketSampleDocument
{
    [BsonElement("externalTargetId")]
    public string ExternalTargetId { get; set; } = string.Empty;

    [BsonElement("mappingVersion")]
    public string MappingVersion { get; set; } = string.Empty;

    [BsonElement("sampleId")]
    public string SampleId { get; set; } = string.Empty;

    [BsonElement("observedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ObservedAtUtc { get; set; }

    [BsonElement("observedAtUtcTicks")]
    public long ObservedAtUtcTicks { get; set; }

    [BsonElement("receivedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReceivedAtUtc { get; set; }

    [BsonElement("receivedAtUtcTicks")]
    public long ReceivedAtUtcTicks { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public LiveOperationalStatus Status { get; set; }

    [BsonElement("queues")]
    public List<LiveQueueObservationDocument> Queues { get; set; } =
        new List<LiveQueueObservationDocument>();
}
