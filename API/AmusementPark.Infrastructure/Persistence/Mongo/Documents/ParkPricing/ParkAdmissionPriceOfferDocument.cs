using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;

[BsonIgnoreExtraElements]
public sealed class ParkAdmissionPriceOfferDocument
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("audienceCategory")]
    public string AudienceCategory { get; set; } = string.Empty;

    [BsonElement("labels")]
    public List<LocalizedTextDocument> Labels { get; set; } = new();

    [BsonElement("onlinePrice")]
    [BsonIgnoreIfNull]
    public ParkPriceValueDocument? OnlinePrice { get; set; }

    [BsonElement("gatePrice")]
    [BsonIgnoreIfNull]
    public ParkPriceValueDocument? GatePrice { get; set; }

    [BsonElement("validFrom")]
    [BsonIgnoreIfNull]
    public string? ValidFrom { get; set; }

    [BsonElement("validTo")]
    [BsonIgnoreIfNull]
    public string? ValidTo { get; set; }

    [BsonElement("purchaseUrl")]
    [BsonIgnoreIfNull]
    public string? PurchaseUrl { get; set; }

    [BsonElement("conditions")]
    public List<LocalizedTextDocument> Conditions { get; set; } = new();

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }
}
