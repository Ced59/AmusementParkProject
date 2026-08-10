using System.Globalization;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkPricing.Services;

public static class ParkPricingNormalizer
{
    private const int MaximumAdmissionOfferCount = 250;
    private const int MaximumAnnualPassCount = 100;
    private const int MaximumParkingOfferCount = 50;
    private const int MaximumHistoricalSnapshotCount = 25;
    private const int MinimumHistoricalYear = 1900;
    private static readonly IReadOnlySet<string> Iso4217CurrencyCodes = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures)
        .Select(static culture => new RegionInfo(culture.Name).ISOCurrencySymbol)
        .Where(static currencyCode => currencyCode.Length == 3)
        .ToHashSet(StringComparer.Ordinal);
    private static readonly IReadOnlySet<string> HistoricalIso4217CurrencyCodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "ATS", "BEF", "CYP", "DEM", "EEK", "ESP", "FIM", "FRF", "GRD", "HRK", "IEP", "ITL",
        "LTL", "LUF", "LVL", "MTL", "NLG", "PTE", "ROL", "SIT", "SKK", "TRL",
    };
    private static readonly IReadOnlyCollection<string> PublicLanguageCodes = new[]
    {
        "fr",
        "en",
        "es",
        "de",
        "it",
        "nl",
        "pt",
        "pl",
    };

    public static ApplicationResult<ParkPricingEntity> Normalize(ParkPricingEntity pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);

        Dictionary<string, IReadOnlyCollection<string>> errors = new(StringComparer.Ordinal);
        ParkPricingEntity normalized = new()
        {
            Id = NormalizeOptionalString(pricing.Id),
            ParkId = NormalizeOptionalString(pricing.ParkId) ?? string.Empty,
            CurrencyCode = (NormalizeOptionalString(pricing.CurrencyCode) ?? string.Empty).ToUpperInvariant(),
            SourceUrl = NormalizeOptionalString(pricing.SourceUrl),
            PurchaseUrl = NormalizeOptionalString(pricing.PurchaseUrl),
            Notes = NormalizeLocalizedTexts(pricing.Notes),
            LastVerifiedAtUtc = pricing.LastVerifiedAtUtc,
            CreatedAtUtc = pricing.CreatedAtUtc,
            UpdatedAtUtc = pricing.UpdatedAtUtc,
        };

        if (string.IsNullOrWhiteSpace(normalized.ParkId))
        {
            errors[nameof(pricing.ParkId)] = new[] { "required" };
        }

        if (!IsValidCurrencyCode(normalized.CurrencyCode))
        {
            errors[nameof(pricing.CurrencyCode)] = new[] { "invalid-iso-4217-code" };
        }

        ValidateOptionalAbsoluteHttpUrl(normalized.SourceUrl, nameof(pricing.SourceUrl), errors);
        ValidateOptionalAbsoluteHttpUrl(normalized.PurchaseUrl, nameof(pricing.PurchaseUrl), errors);
        ValidateOptionalLocalizedTexts(normalized.Notes, nameof(pricing.Notes), errors);

        IReadOnlyCollection<ParkAdmissionPriceOffer> admissionOffers = pricing.AdmissionOffers ?? new List<ParkAdmissionPriceOffer>();
        IReadOnlyCollection<ParkAnnualPassOffer> annualPasses = pricing.AnnualPasses ?? new List<ParkAnnualPassOffer>();
        IReadOnlyCollection<ParkParkingPriceOffer> parkingOffers = pricing.ParkingOffers ?? new List<ParkParkingPriceOffer>();

        if (admissionOffers.Count > MaximumAdmissionOfferCount)
        {
            errors[nameof(pricing.AdmissionOffers)] = new[] { "too-many-offers" };
        }

        if (annualPasses.Count > MaximumAnnualPassCount)
        {
            errors[nameof(pricing.AnnualPasses)] = new[] { "too-many-passes" };
        }

        if (parkingOffers.Count > MaximumParkingOfferCount)
        {
            errors[nameof(pricing.ParkingOffers)] = new[] { "too-many-offers" };
        }

        normalized.AdmissionOffers = NormalizeAdmissionOffers(admissionOffers, nameof(pricing.AdmissionOffers), errors);
        normalized.AnnualPasses = NormalizeAnnualPasses(annualPasses, nameof(pricing.AnnualPasses), errors);
        normalized.ParkingOffers = NormalizeParkingOffers(parkingOffers, nameof(pricing.ParkingOffers), errors);
        normalized.HistoricalSnapshots = NormalizeHistoricalSnapshots(
            pricing.HistoricalSnapshots ?? new List<ParkPricingSnapshot>(),
            errors);

        if (errors.Count > 0)
        {
            return ApplicationResult<ParkPricingEntity>.Failure(ParkPricingApplicationErrors.InvalidPricing(errors));
        }

        return ApplicationResult<ParkPricingEntity>.Success(normalized);
    }

    public static bool HasPublicPricingData(ParkPricingEntity pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);
        return pricing.AdmissionOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)
            || pricing.AnnualPasses.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null)
            || pricing.ParkingOffers.Any(static offer => offer.OnlinePrice is not null || offer.GatePrice is not null);
    }

    private static List<ParkPricingSnapshot> NormalizeHistoricalSnapshots(
        IReadOnlyCollection<ParkPricingSnapshot> snapshots,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        if (snapshots.Count > MaximumHistoricalSnapshotCount)
        {
            errors[nameof(ParkPricingEntity.HistoricalSnapshots)] = new[] { "too-many-snapshots" };
        }

        HashSet<int> usedYears = new();
        List<ParkPricingSnapshot> normalizedSnapshots = new();
        int index = 0;
        foreach (ParkPricingSnapshot snapshot in snapshots)
        {
            string fieldPrefix = $"{nameof(ParkPricingEntity.HistoricalSnapshots)}[{index}]";
            IReadOnlyCollection<ParkAdmissionPriceOffer> admissionOffers = snapshot.AdmissionOffers ?? new List<ParkAdmissionPriceOffer>();
            IReadOnlyCollection<ParkAnnualPassOffer> annualPasses = snapshot.AnnualPasses ?? new List<ParkAnnualPassOffer>();
            IReadOnlyCollection<ParkParkingPriceOffer> parkingOffers = snapshot.ParkingOffers ?? new List<ParkParkingPriceOffer>();
            ParkPricingSnapshot normalized = new()
            {
                Id = NormalizeOptionalString(snapshot.Id) ?? Guid.NewGuid().ToString("N"),
                Year = snapshot.Year,
                CurrencyCode = (NormalizeOptionalString(snapshot.CurrencyCode) ?? string.Empty).ToUpperInvariant(),
                SourceUrl = NormalizeOptionalString(snapshot.SourceUrl),
                Notes = NormalizeLocalizedTexts(snapshot.Notes),
                LastVerifiedAtUtc = snapshot.LastVerifiedAtUtc,
                AdmissionOffers = NormalizeAdmissionOffers(admissionOffers, $"{fieldPrefix}.AdmissionOffers", errors),
                AnnualPasses = NormalizeAnnualPasses(annualPasses, $"{fieldPrefix}.AnnualPasses", errors),
                ParkingOffers = NormalizeParkingOffers(parkingOffers, $"{fieldPrefix}.ParkingOffers", errors),
            };

            if (normalized.Year < MinimumHistoricalYear || normalized.Year > DateTime.MaxValue.Year)
            {
                errors[$"{fieldPrefix}.Year"] = new[] { "invalid-year" };
            }
            else if (!usedYears.Add(normalized.Year))
            {
                errors[$"{fieldPrefix}.Year"] = new[] { "duplicate" };
            }

            if (!IsValidHistoricalCurrencyCode(normalized.CurrencyCode))
            {
                errors[$"{fieldPrefix}.CurrencyCode"] = new[] { "invalid-iso-4217-code" };
            }

            ValidateOptionalAbsoluteHttpUrl(normalized.SourceUrl, $"{fieldPrefix}.SourceUrl", errors);
            ValidateOptionalLocalizedTexts(normalized.Notes, $"{fieldPrefix}.Notes", errors);
            ValidateSnapshotCollectionCounts(normalized, fieldPrefix, errors);
            if (!normalized.HasPricedOffers())
            {
                errors[$"{fieldPrefix}.Offers"] = new[] { "priced-offer-required" };
            }

            normalizedSnapshots.Add(normalized);
            index += 1;
        }

        return normalizedSnapshots
            .OrderByDescending(static snapshot => snapshot.Year)
            .ToList();
    }

    private static void ValidateSnapshotCollectionCounts(
        ParkPricingSnapshot snapshot,
        string fieldPrefix,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        if (snapshot.AdmissionOffers.Count > MaximumAdmissionOfferCount)
        {
            errors[$"{fieldPrefix}.AdmissionOffers"] = new[] { "too-many-offers" };
        }

        if (snapshot.AnnualPasses.Count > MaximumAnnualPassCount)
        {
            errors[$"{fieldPrefix}.AnnualPasses"] = new[] { "too-many-passes" };
        }

        if (snapshot.ParkingOffers.Count > MaximumParkingOfferCount)
        {
            errors[$"{fieldPrefix}.ParkingOffers"] = new[] { "too-many-offers" };
        }
    }

    private static List<ParkAdmissionPriceOffer> NormalizeAdmissionOffers(
        IReadOnlyCollection<ParkAdmissionPriceOffer> offers,
        string collectionFieldPath,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        HashSet<string> usedCodes = new(StringComparer.OrdinalIgnoreCase);
        List<ParkAdmissionPriceOffer> normalizedOffers = new();
        int index = 0;

        foreach (ParkAdmissionPriceOffer offer in offers)
        {
            string fieldPrefix = $"{collectionFieldPath}[{index}]";
            ParkAdmissionPriceOffer normalized = new()
            {
                Id = NormalizeOptionalString(offer.Id) ?? Guid.NewGuid().ToString("N"),
                Code = NormalizeCode(offer.Code),
                AudienceCategory = NormalizeCode(offer.AudienceCategory),
                Labels = NormalizeLocalizedTexts(offer.Labels),
                OnlinePrice = NormalizePrice(offer.OnlinePrice, $"{fieldPrefix}.onlinePrice", errors),
                GatePrice = NormalizePrice(offer.GatePrice, $"{fieldPrefix}.gatePrice", errors),
                ValidFrom = offer.ValidFrom,
                ValidTo = offer.ValidTo,
                PurchaseUrl = NormalizeOptionalString(offer.PurchaseUrl),
                Conditions = NormalizeLocalizedTexts(offer.Conditions),
                SortOrder = offer.SortOrder > 0 ? offer.SortOrder : index + 1,
            };

            ValidateCode(normalized.Code, $"{fieldPrefix}.code", usedCodes, errors);
            ValidateRequiredLocalizedTexts(normalized.Labels, $"{fieldPrefix}.labels", errors);
            if (string.IsNullOrWhiteSpace(normalized.AudienceCategory))
            {
                errors[$"{fieldPrefix}.audienceCategory"] = new[] { "required" };
            }

            ValidatePricePresence(normalized.OnlinePrice, normalized.GatePrice, fieldPrefix, errors);
            ValidateDateRange(normalized.ValidFrom, normalized.ValidTo, fieldPrefix, errors);
            ValidateOptionalAbsoluteHttpUrl(normalized.PurchaseUrl, $"{fieldPrefix}.purchaseUrl", errors);
            ValidateOptionalLocalizedTexts(normalized.Conditions, $"{fieldPrefix}.conditions", errors);
            normalizedOffers.Add(normalized);
            index += 1;
        }

        return normalizedOffers.OrderBy(static item => item.SortOrder).ThenBy(static item => item.Code, StringComparer.Ordinal).ToList();
    }

    private static List<ParkAnnualPassOffer> NormalizeAnnualPasses(
        IReadOnlyCollection<ParkAnnualPassOffer> offers,
        string collectionFieldPath,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        HashSet<string> usedCodes = new(StringComparer.OrdinalIgnoreCase);
        List<ParkAnnualPassOffer> normalizedOffers = new();
        int index = 0;

        foreach (ParkAnnualPassOffer offer in offers)
        {
            string fieldPrefix = $"{collectionFieldPath}[{index}]";
            ParkAnnualPassOffer normalized = new()
            {
                Id = NormalizeOptionalString(offer.Id) ?? Guid.NewGuid().ToString("N"),
                Code = NormalizeCode(offer.Code),
                Names = NormalizeLocalizedTexts(offer.Names),
                OnlinePrice = NormalizePrice(offer.OnlinePrice, $"{fieldPrefix}.onlinePrice", errors),
                GatePrice = NormalizePrice(offer.GatePrice, $"{fieldPrefix}.gatePrice", errors),
                ValidFrom = offer.ValidFrom,
                ValidTo = offer.ValidTo,
                PurchaseUrl = NormalizeOptionalString(offer.PurchaseUrl),
                Conditions = NormalizeLocalizedTexts(offer.Conditions),
                SortOrder = offer.SortOrder > 0 ? offer.SortOrder : index + 1,
            };

            ValidateCode(normalized.Code, $"{fieldPrefix}.code", usedCodes, errors);
            ValidateRequiredLocalizedTexts(normalized.Names, $"{fieldPrefix}.names", errors);

            ValidatePricePresence(normalized.OnlinePrice, normalized.GatePrice, fieldPrefix, errors);
            ValidateDateRange(normalized.ValidFrom, normalized.ValidTo, fieldPrefix, errors);
            ValidateOptionalAbsoluteHttpUrl(normalized.PurchaseUrl, $"{fieldPrefix}.purchaseUrl", errors);
            ValidateOptionalLocalizedTexts(normalized.Conditions, $"{fieldPrefix}.conditions", errors);
            normalizedOffers.Add(normalized);
            index += 1;
        }

        return normalizedOffers.OrderBy(static item => item.SortOrder).ThenBy(static item => item.Code, StringComparer.Ordinal).ToList();
    }

    private static List<ParkParkingPriceOffer> NormalizeParkingOffers(
        IReadOnlyCollection<ParkParkingPriceOffer> offers,
        string collectionFieldPath,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        HashSet<string> usedCodes = new(StringComparer.OrdinalIgnoreCase);
        List<ParkParkingPriceOffer> normalizedOffers = new();
        int index = 0;

        foreach (ParkParkingPriceOffer offer in offers)
        {
            string fieldPrefix = $"{collectionFieldPath}[{index}]";
            ParkParkingPriceOffer normalized = new()
            {
                Id = NormalizeOptionalString(offer.Id) ?? Guid.NewGuid().ToString("N"),
                Code = NormalizeCode(offer.Code),
                Labels = NormalizeLocalizedTexts(offer.Labels),
                OnlinePrice = NormalizePrice(offer.OnlinePrice, $"{fieldPrefix}.onlinePrice", errors),
                GatePrice = NormalizePrice(offer.GatePrice, $"{fieldPrefix}.gatePrice", errors),
                ValidFrom = offer.ValidFrom,
                ValidTo = offer.ValidTo,
                PurchaseUrl = NormalizeOptionalString(offer.PurchaseUrl),
                Conditions = NormalizeLocalizedTexts(offer.Conditions),
                SortOrder = offer.SortOrder > 0 ? offer.SortOrder : index + 1,
            };

            ValidateCode(normalized.Code, $"{fieldPrefix}.code", usedCodes, errors);
            ValidateRequiredLocalizedTexts(normalized.Labels, $"{fieldPrefix}.labels", errors);
            ValidatePricePresence(normalized.OnlinePrice, normalized.GatePrice, fieldPrefix, errors);
            ValidateDateRange(normalized.ValidFrom, normalized.ValidTo, fieldPrefix, errors);
            ValidateOptionalAbsoluteHttpUrl(normalized.PurchaseUrl, $"{fieldPrefix}.purchaseUrl", errors);
            ValidateOptionalLocalizedTexts(normalized.Conditions, $"{fieldPrefix}.conditions", errors);
            normalizedOffers.Add(normalized);
            index += 1;
        }

        return normalizedOffers.OrderBy(static item => item.SortOrder).ThenBy(static item => item.Code, StringComparer.Ordinal).ToList();
    }

    private static ParkPriceValue? NormalizePrice(
        ParkPriceValue? price,
        string fieldPrefix,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        if (price is null)
        {
            return null;
        }

        ParkPriceNormalizationResult result = ParkPriceValueRules.Normalize(price);
        switch (result.Error)
        {
            case null:
                break;
            case ParkPriceValidationError.InvalidMode:
                errors[$"{fieldPrefix}.mode"] = new[] { "invalid" };
                break;
            case ParkPriceValidationError.NegativePrice:
                errors[fieldPrefix] = new[] { "negative-price" };
                break;
            case ParkPriceValidationError.FixedAmountRequired:
                errors[$"{fieldPrefix}.amount"] = new[] { "required" };
                break;
            case ParkPriceValidationError.RangeBoundsRequired:
                errors[fieldPrefix] = new[] { "range-bounds-required" };
                break;
            case ParkPriceValidationError.InvalidRange:
                errors[fieldPrefix] = new[] { "invalid-range" };
                break;
        }

        return result.Value;
    }

    private static void ValidatePricePresence(
        ParkPriceValue? onlinePrice,
        ParkPriceValue? gatePrice,
        string fieldPrefix,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        if (onlinePrice is null && gatePrice is null)
        {
            errors[$"{fieldPrefix}.price"] = new[] { "online-or-gate-price-required" };
        }
    }

    private static void ValidateDateRange(
        DateOnly? validFrom,
        DateOnly? validTo,
        string fieldPrefix,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        if (validFrom.HasValue && validTo.HasValue && validFrom.Value > validTo.Value)
        {
            errors[$"{fieldPrefix}.validity"] = new[] { "invalid-date-range" };
        }
    }

    private static void ValidateCode(
        string code,
        string fieldPath,
        HashSet<string> usedCodes,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            errors[fieldPath] = new[] { "required" };
        }
        else if (!usedCodes.Add(code))
        {
            errors[fieldPath] = new[] { "duplicate" };
        }
    }

    private static void ValidateRequiredLocalizedTexts(
        IReadOnlyCollection<LocalizedText> values,
        string fieldPath,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        ValidateLocalizedTexts(values, fieldPath, false, errors);
    }

    private static void ValidateOptionalLocalizedTexts(
        IReadOnlyCollection<LocalizedText> values,
        string fieldPath,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        ValidateLocalizedTexts(values, fieldPath, true, errors);
    }

    private static void ValidateLocalizedTexts(
        IReadOnlyCollection<LocalizedText> values,
        string fieldPath,
        bool allowEmpty,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        if (allowEmpty && values.Count == 0)
        {
            return;
        }

        HashSet<string> availableLanguages = values
            .Select(static value => value.LanguageCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> missingLanguages = PublicLanguageCodes
            .Where(languageCode => !availableLanguages.Contains(languageCode))
            .Select(static languageCode => $"missing-language:{languageCode}")
            .ToList();
        if (missingLanguages.Count > 0)
        {
            errors[fieldPath] = missingLanguages;
        }
    }

    private static bool IsValidCurrencyCode(string value)
    {
        return Iso4217CurrencyCodes.Contains(value);
    }

    private static bool IsValidHistoricalCurrencyCode(string value)
    {
        return IsValidCurrencyCode(value) || HistoricalIso4217CurrencyCodes.Contains(value);
    }

    private static void ValidateOptionalAbsoluteHttpUrl(
        string? value,
        string fieldPath,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        if (value is null)
        {
            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            errors[fieldPath] = new[] { "invalid-http-url" };
        }
    }

    private static string NormalizeCode(string? value)
    {
        return (NormalizeOptionalString(value) ?? string.Empty).ToLowerInvariant();
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static List<LocalizedText> NormalizeLocalizedTexts(IReadOnlyCollection<LocalizedText>? values)
    {
        Dictionary<string, LocalizedText> result = new(StringComparer.OrdinalIgnoreCase);
        if (values is null)
        {
            return new List<LocalizedText>();
        }

        foreach (LocalizedText value in values)
        {
            string languageCode = NormalizeOptionalString(value.LanguageCode)?.ToLowerInvariant() ?? string.Empty;
            string text = NormalizeOptionalString(value.Value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            result[languageCode] = new LocalizedText(languageCode, text);
        }

        return result.Values.OrderBy(static value => value.LanguageCode, StringComparer.Ordinal).ToList();
    }
}
