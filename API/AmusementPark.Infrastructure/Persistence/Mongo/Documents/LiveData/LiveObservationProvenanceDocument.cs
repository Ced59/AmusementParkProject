using AmusementPark.Core.Domain.LiveData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LiveObservationProvenanceDocument
{
    [BsonElement("externalTargetId")]
    public string ExternalTargetId { get; set; } = string.Empty;

    [BsonElement("observedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ObservedAtUtc { get; set; }

    [BsonElement("receivedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReceivedAtUtc { get; set; }

    [BsonElement("normalizedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NormalizedAtUtc { get; set; }

    [BsonElement("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [BsonElement("adapterVersion")]
    public string AdapterVersion { get; set; } = string.Empty;

    [BsonElement("mappingVersion")]
    public string MappingVersion { get; set; } = string.Empty;

    [BsonElement("confidence")]
    [BsonRepresentation(BsonType.String)]
    public LiveDataConfidence Confidence { get; set; }

    [BsonElement("usagePolicyVersion")]
    public string UsagePolicyVersion { get; set; } = string.Empty;

    [BsonElement("transformationVersion")]
    public string TransformationVersion { get; set; } = string.Empty;
}
