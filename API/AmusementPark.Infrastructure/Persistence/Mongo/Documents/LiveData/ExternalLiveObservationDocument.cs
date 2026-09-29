using AmusementPark.Core.Domain.LiveData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class ExternalLiveObservationDocument
{
    [BsonElement("externalTargetId")]
    public string ExternalTargetId { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    public LiveTargetType TargetType { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public LiveOperationalStatus Status { get; set; }

    [BsonElement("sourceUpdatedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SourceUpdatedAtUtc { get; set; }

    [BsonElement("sourceUpdatedAtUtcTicks")]
    public long SourceUpdatedAtUtcTicks { get; set; }

    [BsonElement("queues")]
    public List<LiveQueueObservationDocument> Queues { get; set; } =
        new List<LiveQueueObservationDocument>();
}
