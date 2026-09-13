using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;

[BsonIgnoreExtraElements]
public sealed class ParkPricingSnapshotDocument
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("currencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

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

    [BsonElement("creditOffers")]
    public List<ParkCreditOfferDocument> CreditOffers { get; set; } = new();
}
