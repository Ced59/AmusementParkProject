using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class WatchPilotDailyMetricsDocument : MongoDocumentBase
{
    [BsonElement("dateUtc")]
    public DateTime DateUtc { get; set; }

    [BsonElement("interactionCounts")]
    public Dictionary<string, long> InteractionCounts { get; set; } = new(StringComparer.Ordinal);

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }
}
