using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;

[BsonIgnoreExtraElements]
public sealed class ParkPricingDocument : MongoDocumentBase
{
    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("currencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("purchaseUrl")]
    [BsonIgnoreIfNull]
    public string? PurchaseUrl { get; set; }

    [BsonElement("notes")]
    public List<LocalizedTextDocument> Notes { get; set; } = new();

    [BsonElement("lastVerifiedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastVerifiedAtUtc { get; set; }

    [BsonElement("admissionOffers")]
    public List<ParkAdmissionPriceOfferDocument> AdmissionOffers { get; set; } = new();

    [BsonElement("annualPasses")]
    public List<ParkAnnualPassOfferDocument> AnnualPasses { get; set; } = new();

    [BsonElement("parkingOffers")]
    public List<ParkParkingPriceOfferDocument> ParkingOffers { get; set; } = new();
}

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

[BsonIgnoreExtraElements]
public sealed class ParkAnnualPassOfferDocument
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("names")]
    public List<LocalizedTextDocument> Names { get; set; } = new();

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

[BsonIgnoreExtraElements]
public sealed class ParkParkingPriceOfferDocument
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

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
