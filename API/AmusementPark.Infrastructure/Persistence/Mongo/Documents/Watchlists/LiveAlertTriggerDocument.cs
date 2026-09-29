using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class LiveAlertTriggerDocument
{
    [BsonElement("type")]
    public LiveAlertType Type { get; set; }

    [BsonElement("previousStatus")]
    [BsonIgnoreIfNull]
    public LiveOperationalStatus? PreviousStatus { get; set; }

    [BsonElement("currentStatus")]
    public LiveOperationalStatus CurrentStatus { get; set; }

    [BsonElement("previousWaitMinutes")]
    [BsonIgnoreIfNull]
    public int? PreviousWaitMinutes { get; set; }

    [BsonElement("currentWaitMinutes")]
    [BsonIgnoreIfNull]
    public int? CurrentWaitMinutes { get; set; }

    [BsonElement("thresholdMinutes")]
    [BsonIgnoreIfNull]
    public int? ThresholdMinutes { get; set; }

    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("observedAt")]
    public DateTime ObservedAt { get; set; }

    [BsonElement("triggeredAt")]
    public DateTime TriggeredAt { get; set; }

    [BsonElement("ageSeconds")]
    public long AgeSeconds { get; set; }
}
