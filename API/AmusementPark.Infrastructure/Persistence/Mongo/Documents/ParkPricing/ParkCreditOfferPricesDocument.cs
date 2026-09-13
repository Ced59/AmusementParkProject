using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;

[BsonIgnoreExtraElements]
public sealed class ParkCreditOfferPricesDocument
{
    [BsonElement("onlinePrice")]
    [BsonIgnoreIfNull]
    public decimal? OnlinePrice { get; set; }

    [BsonElement("gatePrice")]
    [BsonIgnoreIfNull]
    public decimal? GatePrice { get; set; }
}
