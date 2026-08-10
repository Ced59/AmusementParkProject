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
    private static readonly IReadOnlySet<string> Iso4217CurrencyCodes = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures)
        .Select(static culture => new RegionInfo(culture.Name).ISOCurrencySymbol)
        .Where(static currencyCode => currencyCode.Length == 3)
        .ToHashSet(StringComparer.Ordinal);
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

        normalized.AdmissionOffers = NormalizeAdmissionOffers(admissionOffers, errors);
        normalized.AnnualPasses = NormalizeAnnualPasses(annualPasses, errors);
        normalized.ParkingOffers = NormalizeParkingOffers(parkingOffers, errors);

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

    private static List<ParkAdmissionPriceOffer> NormalizeAdmissionOffers(
        IReadOnlyCollection<ParkAdmissionPriceOffer> offers,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        HashSet<string> usedCodes = new(StringComparer.OrdinalIgnoreCase);
        List<ParkAdmissionPriceOffer> normalizedOffers = new();
        int index = 0;

        foreach (ParkAdmissionPriceOffer offer in offers)
        {
            string fieldPrefix = $"{nameof(ParkPricingEntity.AdmissionOffers)}[{index}]";
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
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        HashSet<string> usedCodes = new(StringComparer.OrdinalIgnoreCase);
        List<ParkAnnualPassOffer> normalizedOffers = new();
        int index = 0;

        foreach (ParkAnnualPassOffer offer in offers)
        {
            string fieldPrefix = $"{nameof(ParkPricingEntity.AnnualPasses)}[{index}]";
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
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        HashSet<string> usedCodes = new(StringComparer.OrdinalIgnoreCase);
        List<ParkParkingPriceOffer> normalizedOffers = new();
        int index = 0;

        foreach (ParkParkingPriceOffer offer in offers)
        {
            string fieldPrefix = $"{nameof(ParkPricingEntity.ParkingOffers)}[{index}]";
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

        ParkPriceValue normalized = new()
        {
            Mode = price.Mode,
            Amount = price.Amount,
            MinimumAmount = price.MinimumAmount,
            MaximumAmount = price.MaximumAmount,
        };

        if (!Enum.IsDefined(normalized.Mode))
        {
            errors[$"{fieldPrefix}.mode"] = new[] { "invalid" };
            return normalized;
        }

        if (normalized.Amount < 0 || normalized.MinimumAmount < 0 || normalized.MaximumAmount < 0)
        {
            errors[fieldPrefix] = new[] { "negative-price" };
            return normalized;
        }

        switch (normalized.Mode)
        {
            case ParkPricingMode.Fixed:
                if (!normalized.Amount.HasValue)
                {
                    errors[$"{fieldPrefix}.amount"] = new[] { "required" };
                }

                normalized.MinimumAmount = null;
                normalized.MaximumAmount = null;
                break;

            case ParkPricingMode.Range:
                normalized.Amount = null;
                if (!normalized.MinimumAmount.HasValue || !normalized.MaximumAmount.HasValue)
                {
                    errors[fieldPrefix] = new[] { "range-bounds-required" };
                }
                else if (normalized.MinimumAmount.Value > normalized.MaximumAmount.Value)
                {
                    errors[fieldPrefix] = new[] { "invalid-range" };
                }

                break;

            case ParkPricingMode.Dynamic:
                normalized.Amount = null;
                if (normalized.MinimumAmount.HasValue && normalized.MaximumAmount.HasValue
                    && normalized.MinimumAmount.Value > normalized.MaximumAmount.Value)
                {
                    errors[fieldPrefix] = new[] { "invalid-range" };
                }

                break;
        }

        return normalized;
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
