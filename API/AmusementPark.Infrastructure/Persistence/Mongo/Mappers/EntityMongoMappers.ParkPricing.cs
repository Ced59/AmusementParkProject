using System.Globalization;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static ParkPricingDocument ToDocument(this ParkPricing pricing)
    {
        return new ParkPricingDocument
        {
            Id = string.IsNullOrWhiteSpace(pricing.Id) ? Guid.NewGuid().ToString("N") : pricing.Id,
            ParkId = pricing.ParkId,
            CurrencyCode = pricing.CurrencyCode,
            SourceUrl = pricing.SourceUrl,
            PurchaseUrl = pricing.PurchaseUrl,
            Notes = pricing.Notes,
            LastVerifiedAtUtc = pricing.LastVerifiedAtUtc,
            CreatedAt = pricing.CreatedAtUtc,
            UpdatedAt = pricing.UpdatedAtUtc,
            AdmissionOffers = pricing.AdmissionOffers.Select(static offer => offer.ToDocument()).ToList(),
            AnnualPasses = pricing.AnnualPasses.Select(static offer => offer.ToDocument()).ToList(),
            ParkingOffers = pricing.ParkingOffers.Select(static offer => offer.ToDocument()).ToList(),
        };
    }

    public static ParkPricing ToDomain(this ParkPricingDocument document)
    {
        return new ParkPricing
        {
            Id = document.Id,
            ParkId = document.ParkId,
            CurrencyCode = document.CurrencyCode,
            SourceUrl = document.SourceUrl,
            PurchaseUrl = document.PurchaseUrl,
            Notes = document.Notes,
            LastVerifiedAtUtc = document.LastVerifiedAtUtc,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
            AdmissionOffers = document.AdmissionOffers.Select(static offer => offer.ToDomain()).ToList(),
            AnnualPasses = document.AnnualPasses.Select(static offer => offer.ToDomain()).ToList(),
            ParkingOffers = document.ParkingOffers.Select(static offer => offer.ToDomain()).ToList(),
        };
    }

    private static ParkAdmissionPriceOfferDocument ToDocument(this ParkAdmissionPriceOffer offer)
    {
        return new ParkAdmissionPriceOfferDocument
        {
            Id = string.IsNullOrWhiteSpace(offer.Id) ? Guid.NewGuid().ToString("N") : offer.Id,
            Code = offer.Code,
            AudienceCategory = offer.AudienceCategory,
            Labels = CommonMongoMappers.ToDocuments(offer.Labels),
            OnlinePrice = offer.OnlinePrice?.ToDocument(),
            GatePrice = offer.GatePrice?.ToDocument(),
            ValidFrom = FormatPricingDate(offer.ValidFrom),
            ValidTo = FormatPricingDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDocuments(offer.Conditions),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkAdmissionPriceOffer ToDomain(this ParkAdmissionPriceOfferDocument document)
    {
        return new ParkAdmissionPriceOffer
        {
            Id = document.Id,
            Code = document.Code,
            AudienceCategory = document.AudienceCategory,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            OnlinePrice = document.OnlinePrice?.ToDomain(),
            GatePrice = document.GatePrice?.ToDomain(),
            ValidFrom = ParsePricingDate(document.ValidFrom),
            ValidTo = ParsePricingDate(document.ValidTo),
            PurchaseUrl = document.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDomain(document.Conditions),
            SortOrder = document.SortOrder,
        };
    }

    private static ParkAnnualPassOfferDocument ToDocument(this ParkAnnualPassOffer offer)
    {
        return new ParkAnnualPassOfferDocument
        {
            Id = string.IsNullOrWhiteSpace(offer.Id) ? Guid.NewGuid().ToString("N") : offer.Id,
            Code = offer.Code,
            Names = CommonMongoMappers.ToDocuments(offer.Names),
            OnlinePrice = offer.OnlinePrice?.ToDocument(),
            GatePrice = offer.GatePrice?.ToDocument(),
            ValidFrom = FormatPricingDate(offer.ValidFrom),
            ValidTo = FormatPricingDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDocuments(offer.Conditions),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkAnnualPassOffer ToDomain(this ParkAnnualPassOfferDocument document)
    {
        return new ParkAnnualPassOffer
        {
            Id = document.Id,
            Code = document.Code,
            Names = CommonMongoMappers.ToDomain(document.Names),
            OnlinePrice = document.OnlinePrice?.ToDomain(),
            GatePrice = document.GatePrice?.ToDomain(),
            ValidFrom = ParsePricingDate(document.ValidFrom),
            ValidTo = ParsePricingDate(document.ValidTo),
            PurchaseUrl = document.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDomain(document.Conditions),
            SortOrder = document.SortOrder,
        };
    }

    private static ParkParkingPriceOfferDocument ToDocument(this ParkParkingPriceOffer offer)
    {
        return new ParkParkingPriceOfferDocument
        {
            Id = string.IsNullOrWhiteSpace(offer.Id) ? Guid.NewGuid().ToString("N") : offer.Id,
            Code = offer.Code,
            Labels = CommonMongoMappers.ToDocuments(offer.Labels),
            OnlinePrice = offer.OnlinePrice?.ToDocument(),
            GatePrice = offer.GatePrice?.ToDocument(),
            ValidFrom = FormatPricingDate(offer.ValidFrom),
            ValidTo = FormatPricingDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDocuments(offer.Conditions),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkParkingPriceOffer ToDomain(this ParkParkingPriceOfferDocument document)
    {
        return new ParkParkingPriceOffer
        {
            Id = document.Id,
            Code = document.Code,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            OnlinePrice = document.OnlinePrice?.ToDomain(),
            GatePrice = document.GatePrice?.ToDomain(),
            ValidFrom = ParsePricingDate(document.ValidFrom),
            ValidTo = ParsePricingDate(document.ValidTo),
            PurchaseUrl = document.PurchaseUrl,
            Conditions = CommonMongoMappers.ToDomain(document.Conditions),
            SortOrder = document.SortOrder,
        };
    }

    private static ParkPriceValueDocument ToDocument(this ParkPriceValue price)
    {
        return new ParkPriceValueDocument
        {
            Mode = price.Mode.ToString(),
            Amount = price.Amount,
            MinimumAmount = price.MinimumAmount,
            MaximumAmount = price.MaximumAmount,
        };
    }

    private static ParkPriceValue ToDomain(this ParkPriceValueDocument document)
    {
        return new ParkPriceValue
        {
            Mode = Enum.TryParse(document.Mode, true, out ParkPricingMode mode) && Enum.IsDefined(mode) ? mode : ParkPricingMode.Dynamic,
            Amount = document.Amount,
            MinimumAmount = document.MinimumAmount,
            MaximumAmount = document.MaximumAmount,
        };
    }

    private static string? FormatPricingDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParsePricingDate(string? value)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
            ? parsed
            : null;
    }
}
