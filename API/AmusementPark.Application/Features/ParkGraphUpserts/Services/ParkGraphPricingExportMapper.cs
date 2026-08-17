using System.Globalization;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal static class ParkGraphPricingExportMapper
{
    public static ParkGraphExportPricing Map(ParkPricingEntity pricing)
    {
        return new ParkGraphExportPricing
        {
            ParkId = pricing.ParkId,
            CurrencyCode = pricing.CurrencyCode,
            SourceUrl = pricing.SourceUrl,
            PurchaseUrl = pricing.PurchaseUrl,
            Notes = CopyLocalizedTexts(pricing.Notes),
            LastVerifiedAtUtc = pricing.LastVerifiedAtUtc,
            AdmissionOffers = pricing.AdmissionOffers
                .OrderBy(static offer => offer.SortOrder)
                .ThenBy(static offer => offer.Code, StringComparer.Ordinal)
                .Select(static offer => new ParkGraphExportAdmissionPriceOffer
                {
                    Id = offer.Id,
                    Code = offer.Code,
                    AudienceCategory = offer.AudienceCategory,
                    Labels = CopyLocalizedTexts(offer.Labels),
                    OnlinePrice = MapPriceValue(offer.OnlinePrice),
                    GatePrice = MapPriceValue(offer.GatePrice),
                    ValidFrom = FormatPricingDate(offer.ValidFrom),
                    ValidTo = FormatPricingDate(offer.ValidTo),
                    PurchaseUrl = offer.PurchaseUrl,
                    Conditions = CopyLocalizedTexts(offer.Conditions),
                    SortOrder = offer.SortOrder,
                })
                .ToList(),
            AnnualPasses = pricing.AnnualPasses
                .OrderBy(static offer => offer.SortOrder)
                .ThenBy(static offer => offer.Code, StringComparer.Ordinal)
                .Select(static offer => new ParkGraphExportAnnualPassOffer
                {
                    Id = offer.Id,
                    Code = offer.Code,
                    Names = CopyLocalizedTexts(offer.Names),
                    OnlinePrice = MapPriceValue(offer.OnlinePrice),
                    GatePrice = MapPriceValue(offer.GatePrice),
                    ValidFrom = FormatPricingDate(offer.ValidFrom),
                    ValidTo = FormatPricingDate(offer.ValidTo),
                    PurchaseUrl = offer.PurchaseUrl,
                    Conditions = CopyLocalizedTexts(offer.Conditions),
                    SortOrder = offer.SortOrder,
                })
                .ToList(),
            ParkingOffers = pricing.ParkingOffers
                .OrderBy(static offer => offer.SortOrder)
                .ThenBy(static offer => offer.Code, StringComparer.Ordinal)
                .Select(static offer => new ParkGraphExportParkingPriceOffer
                {
                    Id = offer.Id,
                    Code = offer.Code,
                    Labels = CopyLocalizedTexts(offer.Labels),
                    OnlinePrice = MapPriceValue(offer.OnlinePrice),
                    GatePrice = MapPriceValue(offer.GatePrice),
                    ValidFrom = FormatPricingDate(offer.ValidFrom),
                    ValidTo = FormatPricingDate(offer.ValidTo),
                    PurchaseUrl = offer.PurchaseUrl,
                    Conditions = CopyLocalizedTexts(offer.Conditions),
                    SortOrder = offer.SortOrder,
                })
                .ToList(),
            CreditOffers = pricing.CreditOffers
                .OrderBy(static offer => offer.SortOrder)
                .ThenBy(static offer => offer.UnitCode, StringComparer.Ordinal)
                .ThenBy(static offer => offer.Quantity)
                .Select(static offer => MapCreditOffer(offer))
                .ToList(),
            HistoricalSnapshots = pricing.HistoricalSnapshots
                .OrderByDescending(static snapshot => snapshot.Year)
                .Select(static snapshot => MapSnapshot(snapshot))
                .ToList(),
        };
    }

    private static ParkGraphExportPricingSnapshot MapSnapshot(ParkPricingSnapshot snapshot)
    {
        return new ParkGraphExportPricingSnapshot
        {
            Id = snapshot.Id,
            Year = snapshot.Year,
            CurrencyCode = snapshot.CurrencyCode,
            SourceUrl = snapshot.SourceUrl,
            Notes = CopyLocalizedTexts(snapshot.Notes),
            LastVerifiedAtUtc = snapshot.LastVerifiedAtUtc,
            AdmissionOffers = snapshot.AdmissionOffers
                .OrderBy(static offer => offer.SortOrder)
                .ThenBy(static offer => offer.Code, StringComparer.Ordinal)
                .Select(static offer => new ParkGraphExportAdmissionPriceOffer
                {
                    Id = offer.Id,
                    Code = offer.Code,
                    AudienceCategory = offer.AudienceCategory,
                    Labels = CopyLocalizedTexts(offer.Labels),
                    OnlinePrice = MapPriceValue(offer.OnlinePrice),
                    GatePrice = MapPriceValue(offer.GatePrice),
                    ValidFrom = FormatPricingDate(offer.ValidFrom),
                    ValidTo = FormatPricingDate(offer.ValidTo),
                    PurchaseUrl = offer.PurchaseUrl,
                    Conditions = CopyLocalizedTexts(offer.Conditions),
                    SortOrder = offer.SortOrder,
                })
                .ToList(),
            AnnualPasses = snapshot.AnnualPasses
                .OrderBy(static offer => offer.SortOrder)
                .ThenBy(static offer => offer.Code, StringComparer.Ordinal)
                .Select(static offer => new ParkGraphExportAnnualPassOffer
                {
                    Id = offer.Id,
                    Code = offer.Code,
                    Names = CopyLocalizedTexts(offer.Names),
                    OnlinePrice = MapPriceValue(offer.OnlinePrice),
                    GatePrice = MapPriceValue(offer.GatePrice),
                    ValidFrom = FormatPricingDate(offer.ValidFrom),
                    ValidTo = FormatPricingDate(offer.ValidTo),
                    PurchaseUrl = offer.PurchaseUrl,
                    Conditions = CopyLocalizedTexts(offer.Conditions),
                    SortOrder = offer.SortOrder,
                })
                .ToList(),
            ParkingOffers = snapshot.ParkingOffers
                .OrderBy(static offer => offer.SortOrder)
                .ThenBy(static offer => offer.Code, StringComparer.Ordinal)
                .Select(static offer => new ParkGraphExportParkingPriceOffer
                {
                    Id = offer.Id,
                    Code = offer.Code,
                    Labels = CopyLocalizedTexts(offer.Labels),
                    OnlinePrice = MapPriceValue(offer.OnlinePrice),
                    GatePrice = MapPriceValue(offer.GatePrice),
                    ValidFrom = FormatPricingDate(offer.ValidFrom),
                    ValidTo = FormatPricingDate(offer.ValidTo),
                    PurchaseUrl = offer.PurchaseUrl,
                    Conditions = CopyLocalizedTexts(offer.Conditions),
                    SortOrder = offer.SortOrder,
                })
                .ToList(),
            CreditOffers = snapshot.CreditOffers
                .OrderBy(static offer => offer.SortOrder)
                .ThenBy(static offer => offer.UnitCode, StringComparer.Ordinal)
                .ThenBy(static offer => offer.Quantity)
                .Select(static offer => MapCreditOffer(offer))
                .ToList(),
        };
    }

    private static ParkGraphExportCreditOffer MapCreditOffer(ParkCreditOffer offer)
    {
        return new ParkGraphExportCreditOffer
        {
            Id = offer.Id,
            UnitCode = offer.UnitCode,
            Quantity = offer.Quantity,
            Labels = CopyLocalizedTexts(offer.Labels),
            Prices = new ParkGraphExportCreditOfferPrices
            {
                OnlinePrice = offer.Prices.OnlinePrice,
                GatePrice = offer.Prices.GatePrice,
            },
            ValidFrom = FormatPricingDate(offer.ValidFrom),
            ValidTo = FormatPricingDate(offer.ValidTo),
            PurchaseUrl = offer.PurchaseUrl,
            Conditions = CopyLocalizedTexts(offer.Conditions),
            SortOrder = offer.SortOrder,
        };
    }

    private static ParkGraphExportPriceValue? MapPriceValue(ParkPriceValue? price)
    {
        return price is null
            ? null
            : new ParkGraphExportPriceValue
            {
                Mode = price.Mode,
                Amount = price.Amount,
                MinimumAmount = price.MinimumAmount,
                MaximumAmount = price.MaximumAmount,
            };
    }

    private static string? FormatPricingDate(DateOnly? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static List<LocalizedText> CopyLocalizedTexts(IReadOnlyCollection<LocalizedText> values)
    {
        return values
            .OrderBy(static value => value.LanguageCode, StringComparer.Ordinal)
            .Select(static value => new LocalizedText(value.LanguageCode, value.Value))
            .ToList();
    }
}
