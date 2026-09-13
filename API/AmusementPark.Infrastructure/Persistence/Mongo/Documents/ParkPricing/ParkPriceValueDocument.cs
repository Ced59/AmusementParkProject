using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;

[BsonIgnoreExtraElements]
public sealed class ParkPriceValueDocument
{
    [BsonElement("mode")]
    public string Mode { get; set; } = string.Empty;

    [BsonElement("amount")]
    [BsonIgnoreIfNull]
    public decimal? Amount { get; set; }

    [BsonElement("minimumAmount")]
    [BsonIgnoreIfNull]
    public decimal? MinimumAmount { get; set; }

    [BsonElement("maximumAmount")]
    [BsonIgnoreIfNull]
    public decimal? MaximumAmount { get; set; }
}
