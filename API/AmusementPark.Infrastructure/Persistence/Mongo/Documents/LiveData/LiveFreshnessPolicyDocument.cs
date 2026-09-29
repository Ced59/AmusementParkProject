using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LiveFreshnessPolicyDocument
{
    [BsonElement("version")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("freshUntilMilliseconds")]
    public long FreshUntilMilliseconds { get; set; }

    [BsonElement("agingUntilMilliseconds")]
    public long AgingUntilMilliseconds { get; set; }

    [BsonElement("staleUntilMilliseconds")]
    public long StaleUntilMilliseconds { get; set; }

    [BsonElement("acceptedFutureSkewMilliseconds")]
    public long AcceptedFutureSkewMilliseconds { get; set; }
}
