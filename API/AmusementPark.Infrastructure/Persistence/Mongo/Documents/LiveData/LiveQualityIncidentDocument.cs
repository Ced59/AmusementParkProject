using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LiveQualityIncidentDocument : MongoDocumentBase
{
    [BsonElement("incidentId")]
    public string IncidentId { get; set; } = string.Empty;

    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("observation")]
    [BsonIgnoreIfNull]
    public ExternalLiveObservationDocument? Observation { get; set; }

    [BsonElement("reason")]
    [BsonRepresentation(BsonType.String)]
    public LiveQualityIncidentReason Reason { get; set; }

    [BsonElement("diagnosticCode")]
    [BsonIgnoreIfNull]
    public string? DiagnosticCode { get; set; }

    [BsonElement("diagnosticExternalTargetId")]
    [BsonIgnoreIfNull]
    public string? DiagnosticExternalTargetId { get; set; }

    [BsonElement("diagnosticField")]
    [BsonIgnoreIfNull]
    public string? DiagnosticField { get; set; }

    [BsonElement("receivedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReceivedAtUtc { get; set; }

    [BsonElement("receivedAtUtcTicks")]
    public long ReceivedAtUtcTicks { get; set; }

    [BsonElement("detectedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DetectedAtUtc { get; set; }

    [BsonElement("detectedAtUtcTicks")]
    public long DetectedAtUtcTicks { get; set; }

    [BsonElement("expiresAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [BsonElement("adapterVersion")]
    public string AdapterVersion { get; set; } = string.Empty;

    [BsonElement("usagePolicyVersion")]
    public string UsagePolicyVersion { get; set; } = string.Empty;

    [BsonElement("transformationVersion")]
    public string TransformationVersion { get; set; } = string.Empty;

    [BsonElement("confidence")]
    [BsonRepresentation(BsonType.String)]
    public LiveDataConfidence Confidence { get; set; }

    [BsonElement("freshnessPolicy")]
    public LiveFreshnessPolicyDocument FreshnessPolicy { get; set; } =
        new LiveFreshnessPolicyDocument();

    [BsonElement("payloadSha256")]
    [BsonIgnoreIfNull]
    public string? PayloadSha256 { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public LiveQualityIncidentStatus Status { get; set; }

    [BsonElement("resolvedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ResolvedAtUtc { get; set; }

    [BsonElement("resolvedByUserId")]
    [BsonIgnoreIfNull]
    public string? ResolvedByUserId { get; set; }

    [BsonElement("replayAttemptCount")]
    public int ReplayAttemptCount { get; set; }

    [BsonElement("lastReplayAttemptAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? LastReplayAttemptAtUtc { get; set; }
}
