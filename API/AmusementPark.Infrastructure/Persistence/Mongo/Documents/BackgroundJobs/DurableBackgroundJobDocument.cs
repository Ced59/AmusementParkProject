using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;

[BsonIgnoreExtraElements]
public sealed class DurableBackgroundJobDocument : MongoDocumentBase
{
    [BsonElement("kind")]
    public string Kind { get; set; } = string.Empty;

    [BsonElement("naturalKey")]
    [BsonIgnoreIfNull]
    public string? NaturalKey { get; set; }

    [BsonElement("idempotencyKey")]
    [BsonIgnoreIfNull]
    public string? IdempotencyKey { get; set; }

    [BsonElement("payloadVersion")]
    public int PayloadVersion { get; set; }

    [BsonElement("payload")]
    public BsonDocument Payload { get; set; } = new BsonDocument();

    [BsonElement("requestedRevision")]
    [BsonIgnoreIfNull]
    public long? RequestedRevision { get; set; }

    [BsonElement("processedRevision")]
    [BsonIgnoreIfNull]
    public long? ProcessedRevision { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public DurableBackgroundJobStatus Status { get; set; }

    [BsonElement("priority")]
    public int Priority { get; set; }

    [BsonElement("attemptCount")]
    public int AttemptCount { get; set; }

    [BsonElement("notBeforeUtc")]
    public DateTime NotBeforeUtc { get; set; }

    [BsonElement("leaseOwner")]
    [BsonIgnoreIfNull]
    public string? LeaseOwner { get; set; }

    [BsonElement("leaseToken")]
    [BsonIgnoreIfNull]
    public string? LeaseToken { get; set; }

    [BsonElement("leaseExpiresAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LeaseExpiresAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("lastErrorCode")]
    [BsonIgnoreIfNull]
    public string? LastErrorCode { get; set; }

    [BsonElement("correlationId")]
    [BsonIgnoreIfNull]
    public string? CorrelationId { get; set; }
}
