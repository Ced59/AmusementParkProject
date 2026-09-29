using AmusementPark.Core.Domain.LiveData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LiveQueueObservationDocument
{
    [BsonElement("kind")]
    [BsonRepresentation(BsonType.String)]
    public LiveQueueKind Kind { get; set; }

    [BsonElement("waitTimeMinutes")]
    [BsonIgnoreIfNull]
    public int? WaitTimeMinutes { get; set; }

    [BsonElement("isEstimated")]
    public bool IsEstimated { get; set; }

    [BsonElement("availability")]
    [BsonRepresentation(BsonType.String)]
    public LiveQueueAvailability Availability { get; set; }

    [BsonElement("returnStartUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ReturnStartUtc { get; set; }

    [BsonElement("returnEndUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ReturnEndUtc { get; set; }

    [BsonElement("currentGroupStart")]
    [BsonIgnoreIfNull]
    public int? CurrentGroupStart { get; set; }

    [BsonElement("currentGroupEnd")]
    [BsonIgnoreIfNull]
    public int? CurrentGroupEnd { get; set; }

    [BsonElement("nextAllocationUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? NextAllocationUtc { get; set; }

    [BsonElement("priceMinorUnits")]
    [BsonIgnoreIfNull]
    public long? PriceMinorUnits { get; set; }

    [BsonElement("currencyCode")]
    [BsonIgnoreIfNull]
    public string? CurrencyCode { get; set; }
}
