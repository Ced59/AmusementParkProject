using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Core.Geo;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Parks.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System.Text;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;
internal static class ParkGraphUpsertProcessorPricingExtensions
{
    internal static async Task ProcessPricingAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, Park targetPark, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!ParkGraphUpsertProcessorPricingExtensions.HasPricingPatch(root))
        {
            return;
        }

        JsonElement? patch = ParkGraphUpsertProcessorPricingExtensions.ResolvePricingPatch(root);
        ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange(ParkGraphUpsertProcessor.PricingEntityType, targetPark.Id, ParkGraphUpsertProcessor.PricingPropertyName, string.IsNullOrWhiteSpace(targetPark.Name) ? targetPark.Id : $"{targetPark.Name} pricing", "Unchanged", ParkGraphUpsertProcessor.PricingPropertyName);
        if (patch is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("pricing doit être un objet JSON.");
            return;
        }

        if (!ParkGraphUpsertProcessorPricingExtensions.HasPricingData(patch.Value))
        {
            return;
        }

        if (!targetPark.Status.IsOpenToVisitors())
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add($"pricing est réservé aux parcs dont le statut est '{ParkStatus.Operating}'. Le parc cible utilise '{targetPark.Status}'.");
            return;
        }

        if (processorContext.parkPricingRepository is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("Le traitement des tarifs n'est pas disponible dans ce contexte.");
            return;
        }

        List<string> readErrors = new();
        ParkPricingEntity pricing = ParkGraphUpsertProcessorPricingExtensions.ReadPricing(patch.Value, targetPark.Id, readErrors);
        string? requestedParkId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "parkId");
        if (!string.IsNullOrWhiteSpace(requestedParkId) && !string.Equals(requestedParkId, targetPark.Id, StringComparison.Ordinal))
        {
            readErrors.Add($"pricing.parkId pointe vers '{requestedParkId}' mais le parc cible est '{targetPark.Id}'.");
        }

        if (readErrors.Count > 0)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.AddRange(readErrors);
            return;
        }

        ParkPricingEntity? existingPricing = await processorContext.parkPricingRepository.GetByParkIdAsync(targetPark.Id, cancellationToken);
        if (existingPricing is not null)
        {
            if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "historicalSnapshots"))
            {
                pricing.HistoricalSnapshots = existingPricing.HistoricalSnapshots;
            }

            if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "creditOffers"))
            {
                pricing.CreditOffers = existingPricing.CreditOffers;
            }
        }

        ApplicationResult<ParkPricingEntity> normalizedResult = ParkPricingNormalizer.Normalize(pricing);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            ParkGraphUpsertProcessorPricingExtensions.AddPricingValidationErrors(result, normalizedResult);
            return;
        }

        ParkPricingEntity normalizedPricing = normalizedResult.Value;
        bool isNew = existingPricing is null || !ParkPricingNormalizer.HasPublicPricingData(existingPricing);
        ParkGraphUpsertProcessorPricingExtensions.AddPricingChanges(change, existingPricing, normalizedPricing);
        if (change.Fields.Count > 0 || isNew)
        {
            change.ChangeType = isNew ? "Created" : "Updated";
        }

        result.Changes.Add(change);
        if (apply)
        {
            await processorContext.parkPricingRepository.UpsertAsync(normalizedPricing, cancellationToken);
        }
    }

    internal static bool HasPricingPatch(JsonElement root)
    {
        return (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(root, ParkGraphUpsertProcessor.PricingPropertyName) && !ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(root, ParkGraphUpsertProcessor.PricingPropertyName)) || (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(root, ParkGraphUpsertProcessor.LegacyPricingPropertyName) && !ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(root, ParkGraphUpsertProcessor.LegacyPricingPropertyName));
    }

    internal static JsonElement? ResolvePricingPatch(JsonElement? root)
    {
        return ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, ParkGraphUpsertProcessor.PricingPropertyName) ?? ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, ParkGraphUpsertProcessor.LegacyPricingPropertyName);
    }

    internal static bool HasPricingData(JsonElement patch)
    {
        return ParkGraphUpsertProcessorPricingExtensions.HasNonEmptyPricingArray(patch, "admissionOffers") || ParkGraphUpsertProcessorPricingExtensions.HasNonEmptyPricingArray(patch, "annualPasses") || ParkGraphUpsertProcessorPricingExtensions.HasNonEmptyPricingArray(patch, "parkingOffers") || ParkGraphUpsertProcessorPricingExtensions.HasNonEmptyPricingArray(patch, "creditOffers") || ParkGraphUpsertProcessorPricingExtensions.HasNonEmptyPricingArray(patch, "historicalSnapshots");
    }

    internal static bool HasNonEmptyPricingArray(JsonElement patch, string propertyName)
    {
        if (!patch.TryGetProperty(propertyName, out JsonElement value))
        {
            return false;
        }

        return value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 0;
    }

    internal static ParkPricingEntity ReadPricing(JsonElement patch, string targetParkId, List<string> errors)
    {
        return new ParkPricingEntity
        {
            ParkId = targetParkId,
            CurrencyCode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "currencyCode") ?? string.Empty,
            SourceUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "sourceUrl"),
            PurchaseUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "purchaseUrl"),
            Notes = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(patch, "notes", "pricing", errors),
            LastVerifiedAtUtc = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingUtcDate(patch, "lastVerifiedAtUtc", "pricing", errors),
            AdmissionOffers = ParkGraphUpsertProcessorPricingExtensions.ReadAdmissionOffers(patch, "pricing", errors),
            AnnualPasses = ParkGraphUpsertProcessorPricingExtensions.ReadAnnualPasses(patch, "pricing", errors),
            ParkingOffers = ParkGraphUpsertProcessorPricingExtensions.ReadParkingOffers(patch, "pricing", errors),
            CreditOffers = ParkGraphUpsertProcessorPricingExtensions.ReadCreditOffers(patch, "pricing", errors),
            HistoricalSnapshots = ParkGraphUpsertProcessorPricingExtensions.ReadHistoricalSnapshots(patch, errors),
        };
    }

    internal static List<ParkPricingSnapshot> ReadHistoricalSnapshots(JsonElement patch, List<string> errors)
    {
        JsonElement? array = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "historicalSnapshots");
        if (array is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "historicalSnapshots"))
            {
                errors.Add("pricing.historicalSnapshots doit être un tableau.");
            }

            return new List<ParkPricingSnapshot>();
        }

        List<ParkPricingSnapshot> snapshots = new();
        int index = 0;
        foreach (JsonElement element in array.Value.EnumerateArray())
        {
            string prefix = $"pricing.historicalSnapshots[{index}]";
            if (element.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{prefix} doit être un objet.");
                index += 1;
                continue;
            }

            snapshots.Add(new ParkPricingSnapshot { Id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "id"), Year = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(element, "year") ?? 0, CurrencyCode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "currencyCode") ?? string.Empty, SourceUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "sourceUrl"), Notes = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(element, "notes", prefix, errors), LastVerifiedAtUtc = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingUtcDate(element, "lastVerifiedAtUtc", prefix, errors), AdmissionOffers = ParkGraphUpsertProcessorPricingExtensions.ReadAdmissionOffers(element, prefix, errors), AnnualPasses = ParkGraphUpsertProcessorPricingExtensions.ReadAnnualPasses(element, prefix, errors), ParkingOffers = ParkGraphUpsertProcessorPricingExtensions.ReadParkingOffers(element, prefix, errors), CreditOffers = ParkGraphUpsertProcessorPricingExtensions.ReadCreditOffers(element, prefix, errors), });
            index += 1;
        }

        return snapshots;
    }

    internal static List<ParkAdmissionPriceOffer> ReadAdmissionOffers(JsonElement patch, string rootPrefix, List<string> errors)
    {
        JsonElement? array = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "admissionOffers");
        if (array is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "admissionOffers"))
            {
                errors.Add($"{rootPrefix}.admissionOffers doit être un tableau.");
            }

            return new List<ParkAdmissionPriceOffer>();
        }

        List<ParkAdmissionPriceOffer> offers = new();
        int index = 0;
        foreach (JsonElement element in array.Value.EnumerateArray())
        {
            string prefix = $"{rootPrefix}.admissionOffers[{index}]";
            if (element.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{prefix} doit être un objet.");
                index += 1;
                continue;
            }

            offers.Add(new ParkAdmissionPriceOffer { Id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "id"), Code = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "code") ?? string.Empty, AudienceCategory = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "audienceCategory") ?? string.Empty, Labels = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(element, "labels", prefix, errors), OnlinePrice = ParkGraphUpsertProcessorPricingExtensions.ReadPriceValue(element, "onlinePrice", prefix, errors), GatePrice = ParkGraphUpsertProcessorPricingExtensions.ReadPriceValue(element, "gatePrice", prefix, errors), ValidFrom = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDate(element, "validFrom", prefix, errors), ValidTo = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDate(element, "validTo", prefix, errors), PurchaseUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "purchaseUrl"), Conditions = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(element, "conditions", prefix, errors), SortOrder = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(element, "sortOrder") ?? index + 1, });
            index += 1;
        }

        return offers;
    }

    internal static List<ParkAnnualPassOffer> ReadAnnualPasses(JsonElement patch, string rootPrefix, List<string> errors)
    {
        JsonElement? array = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "annualPasses");
        if (array is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "annualPasses"))
            {
                errors.Add($"{rootPrefix}.annualPasses doit être un tableau.");
            }

            return new List<ParkAnnualPassOffer>();
        }

        List<ParkAnnualPassOffer> offers = new();
        int index = 0;
        foreach (JsonElement element in array.Value.EnumerateArray())
        {
            string prefix = $"{rootPrefix}.annualPasses[{index}]";
            if (element.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{prefix} doit être un objet.");
                index += 1;
                continue;
            }

            offers.Add(new ParkAnnualPassOffer { Id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "id"), Code = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "code") ?? string.Empty, Names = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(element, "names", prefix, errors), OnlinePrice = ParkGraphUpsertProcessorPricingExtensions.ReadPriceValue(element, "onlinePrice", prefix, errors), GatePrice = ParkGraphUpsertProcessorPricingExtensions.ReadPriceValue(element, "gatePrice", prefix, errors), ValidFrom = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDate(element, "validFrom", prefix, errors), ValidTo = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDate(element, "validTo", prefix, errors), PurchaseUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "purchaseUrl"), Conditions = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(element, "conditions", prefix, errors), SortOrder = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(element, "sortOrder") ?? index + 1, });
            index += 1;
        }

        return offers;
    }

    internal static List<ParkParkingPriceOffer> ReadParkingOffers(JsonElement patch, string rootPrefix, List<string> errors)
    {
        JsonElement? array = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "parkingOffers");
        if (array is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "parkingOffers"))
            {
                errors.Add($"{rootPrefix}.parkingOffers doit être un tableau.");
            }

            return new List<ParkParkingPriceOffer>();
        }

        List<ParkParkingPriceOffer> offers = new();
        int index = 0;
        foreach (JsonElement element in array.Value.EnumerateArray())
        {
            string prefix = $"{rootPrefix}.parkingOffers[{index}]";
            if (element.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{prefix} doit être un objet.");
                index += 1;
                continue;
            }

            offers.Add(new ParkParkingPriceOffer { Id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "id"), Code = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "code") ?? string.Empty, Labels = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(element, "labels", prefix, errors), OnlinePrice = ParkGraphUpsertProcessorPricingExtensions.ReadPriceValue(element, "onlinePrice", prefix, errors), GatePrice = ParkGraphUpsertProcessorPricingExtensions.ReadPriceValue(element, "gatePrice", prefix, errors), ValidFrom = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDate(element, "validFrom", prefix, errors), ValidTo = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDate(element, "validTo", prefix, errors), PurchaseUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "purchaseUrl"), Conditions = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(element, "conditions", prefix, errors), SortOrder = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(element, "sortOrder") ?? index + 1, });
            index += 1;
        }

        return offers;
    }

    internal static List<ParkCreditOffer> ReadCreditOffers(JsonElement patch, string rootPrefix, List<string> errors)
    {
        JsonElement? array = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "creditOffers");
        if (array is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "creditOffers"))
            {
                errors.Add($"{rootPrefix}.creditOffers doit être un tableau.");
            }

            return new List<ParkCreditOffer>();
        }

        List<ParkCreditOffer> offers = new();
        int index = 0;
        foreach (JsonElement element in array.Value.EnumerateArray())
        {
            string prefix = $"{rootPrefix}.creditOffers[{index}]";
            if (element.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{prefix} doit être un objet.");
                index += 1;
                continue;
            }

            JsonElement? prices = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(element, "prices");
            if (prices is null && ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, "prices"))
            {
                errors.Add($"{prefix}.prices doit être un objet.");
            }

            offers.Add(new ParkCreditOffer { Id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "id"), UnitCode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "unitCode") ?? string.Empty, Quantity = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(element, "quantity") ?? 0, Labels = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(element, "labels", prefix, errors), Prices = new ParkCreditOfferPrices { OnlinePrice = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDecimal(prices, "onlinePrice", $"{prefix}.prices", errors), GatePrice = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDecimal(prices, "gatePrice", $"{prefix}.prices", errors), }, ValidFrom = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDate(element, "validFrom", prefix, errors), ValidTo = ParkGraphUpsertProcessorPricingExtensions.ReadOptionalPricingDate(element, "validTo", prefix, errors), PurchaseUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "purchaseUrl"), Conditions = ParkGraphUpsertProcessorPricingExtensions.ReadPricingLocalizedTexts(element, "conditions", prefix, errors), SortOrder = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(element, "sortOrder") ?? index + 1, });
            index += 1;
        }

        return offers;
    }

    internal static decimal? ReadOptionalPricingDecimal(JsonElement? element, string propertyName, string prefix, List<string> errors)
    {
        if (element is null || !element.Value.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal result))
        {
            return result;
        }

        errors.Add($"{prefix}.{propertyName} doit être un nombre décimal.");
        return null;
    }

    internal static List<AmusementPark.Core.Localization.LocalizedText> ReadPricingLocalizedTexts(JsonElement element, string propertyName, string prefix, List<string> errors)
    {
        JsonElement? array = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(element, propertyName);
        if (array is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, propertyName))
            {
                errors.Add($"{prefix}.{propertyName} doit être un tableau.");
            }

            return new List<AmusementPark.Core.Localization.LocalizedText>();
        }

        return ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(array);
    }

    internal static ParkPriceValue? ReadPriceValue(JsonElement element, string propertyName, string prefix, List<string> errors)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, propertyName) || ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(element, propertyName))
        {
            return null;
        }

        JsonElement? priceElement = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(element, propertyName);
        if (priceElement is null)
        {
            errors.Add($"{prefix}.{propertyName} doit être un objet.");
            return null;
        }

        string fieldPrefix = $"{prefix}.{propertyName}";
        string? modeValue = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(priceElement, "mode");
        ParkPricingMode mode = ParkPricingMode.Fixed;
        if (string.IsNullOrWhiteSpace(modeValue) || !Enum.TryParse(modeValue, true, out mode) || !Enum.IsDefined(mode))
        {
            errors.Add($"{fieldPrefix}.mode doit valoir Fixed, Range ou Dynamic.");
        }

        return new ParkPriceValue
        {
            Mode = mode,
            Amount = ParkGraphUpsertProcessorPricingExtensions.ReadPricingDecimal(priceElement.Value, "amount", $"{fieldPrefix}.amount", errors),
            MinimumAmount = ParkGraphUpsertProcessorPricingExtensions.ReadPricingDecimal(priceElement.Value, "minimumAmount", $"{fieldPrefix}.minimumAmount", errors),
            MaximumAmount = ParkGraphUpsertProcessorPricingExtensions.ReadPricingDecimal(priceElement.Value, "maximumAmount", $"{fieldPrefix}.maximumAmount", errors),
        };
    }

    internal static decimal? ReadPricingDecimal(JsonElement element, string propertyName, string fieldPath, List<string> errors)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal numericValue))
        {
            return numericValue;
        }

        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal stringValue))
        {
            return stringValue;
        }

        errors.Add($"{fieldPath} doit être un nombre décimal.");
        return null;
    }

    internal static DateOnly? ReadOptionalPricingDate(JsonElement element, string propertyName, string prefix, List<string> errors)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, propertyName) || ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(element, propertyName))
        {
            return null;
        }

        string? value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, propertyName);
        if (DateOnly.TryParseExact(value, ParkGraphUpsertProcessor.PricingDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            return date;
        }

        errors.Add($"{prefix}.{propertyName} doit utiliser le format {ParkGraphUpsertProcessor.PricingDateFormat}.");
        return null;
    }

    internal static DateTime? ReadOptionalPricingUtcDate(JsonElement element, string propertyName, string prefix, List<string> errors)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, propertyName) || ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(element, propertyName))
        {
            return null;
        }

        string? value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, propertyName);
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsed))
        {
            return parsed.UtcDateTime;
        }

        errors.Add($"{prefix}.{propertyName} doit être une date ISO 8601 valide.");
        return null;
    }

    internal static void AddPricingValidationErrors(ParkGraphUpsertResult result, ApplicationResult<ParkPricingEntity> normalizedResult)
    {
        foreach (ApplicationError error in normalizedResult.Errors)
        {
            if (error.Details is null || error.Details.Count == 0)
            {
                result.Errors.Add($"pricing: {error.Message}");
                continue;
            }

            foreach (KeyValuePair<string, IReadOnlyCollection<string>> detail in error.Details)
            {
                result.Errors.Add($"pricing.{detail.Key}: {string.Join(", ", detail.Value)}");
            }
        }
    }

    internal static void AddPricingChanges(ParkGraphUpsertChange change, ParkPricingEntity? existingPricing, ParkPricingEntity normalizedPricing)
    {
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.currencyCode", existingPricing?.CurrencyCode, normalizedPricing.CurrencyCode);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.sourceUrl", existingPricing?.SourceUrl, normalizedPricing.SourceUrl);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.purchaseUrl", existingPricing?.PurchaseUrl, normalizedPricing.PurchaseUrl);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.notes", ParkGraphUpsertProcessorPricingExtensions.DescribePricing(existingPricing?.Notes), ParkGraphUpsertProcessorPricingExtensions.DescribePricing(normalizedPricing.Notes));
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.lastVerifiedAtUtc", existingPricing?.LastVerifiedAtUtc, normalizedPricing.LastVerifiedAtUtc);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.admissionOffers", ParkGraphUpsertProcessorPricingExtensions.DescribePricing(existingPricing?.AdmissionOffers), ParkGraphUpsertProcessorPricingExtensions.DescribePricing(normalizedPricing.AdmissionOffers));
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.annualPasses", ParkGraphUpsertProcessorPricingExtensions.DescribePricing(existingPricing?.AnnualPasses), ParkGraphUpsertProcessorPricingExtensions.DescribePricing(normalizedPricing.AnnualPasses));
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.parkingOffers", ParkGraphUpsertProcessorPricingExtensions.DescribePricing(existingPricing?.ParkingOffers), ParkGraphUpsertProcessorPricingExtensions.DescribePricing(normalizedPricing.ParkingOffers));
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.creditOffers", ParkGraphUpsertProcessorPricingExtensions.DescribePricing(existingPricing?.CreditOffers), ParkGraphUpsertProcessorPricingExtensions.DescribePricing(normalizedPricing.CreditOffers));
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "pricing.historicalSnapshots", ParkGraphUpsertProcessorPricingExtensions.DescribePricing(existingPricing?.HistoricalSnapshots), ParkGraphUpsertProcessorPricingExtensions.DescribePricing(normalizedPricing.HistoricalSnapshots));
    }

    internal static string DescribePricing<T>(IReadOnlyCollection<T>? values)
    {
        return values is null || values.Count == 0 ? string.Empty : JsonSerializer.Serialize(values);
    }
}
