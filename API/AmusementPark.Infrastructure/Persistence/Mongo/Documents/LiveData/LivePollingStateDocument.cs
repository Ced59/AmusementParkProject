using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LivePollingStateDocument : MongoDocumentBase
{
    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("externalEntityId")]
    public string ExternalEntityId { get; set; } = string.Empty;

    [BsonElement("entityTag")]
    [BsonIgnoreIfNull]
    public string? EntityTag { get; set; }

    [BsonElement("nextAttemptAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? NextAttemptAtUtc { get; set; }

    [BsonElement("lastPolledAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? LastPolledAtUtc { get; set; }

    [BsonElement("lastSuccessfulPollAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? LastSuccessfulPollAtUtc { get; set; }

    [BsonElement("consecutiveFailures")]
    public int ConsecutiveFailures { get; set; }

    [BsonElement("circuitOpenUntilUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? CircuitOpenUntilUtc { get; set; }

    [BsonElement("lastDisposition")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public LivePollingCompletionDisposition? LastDisposition { get; set; }

    [BsonElement("leaseOwner")]
    [BsonIgnoreIfNull]
    public string? LeaseOwner { get; set; }

    [BsonElement("leaseToken")]
    [BsonIgnoreIfNull]
    public string? LeaseToken { get; set; }

    [BsonElement("leaseExpiresAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? LeaseExpiresAtUtc { get; set; }
}
