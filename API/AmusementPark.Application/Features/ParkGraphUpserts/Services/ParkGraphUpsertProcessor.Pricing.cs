using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private const string PricingEntityType = "ParkPricing";
    private const string PricingPropertyName = "pricing";
    private const string LegacyPricingPropertyName = "parkPricing";
    private const string PricingDateFormat = "yyyy-MM-dd";

    private readonly IParkPricingRepository? parkPricingRepository;

    /// <summary>
    /// Constructor used by dependency injection when park pricing support is available.
    /// The legacy constructor remains available so existing focused tests can keep supplying
    /// only the dependencies required by the scenario they exercise.
    /// </summary>
    public ParkGraphUpsertProcessor(
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IParkItemRepository parkItemRepository,
        IParkFounderRepository parkFounderRepository,
        IParkOperatorRepository parkOperatorRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        IImageRepository imageRepository,
        IRemoteImageImporter remoteImageImporter,
        ISearchProjectionWriter searchProjectionWriter,
        IParkGraphUpsertHistoryRepository historyRepository,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier,
        IMeasurementConversionService measurementConversionService,
        IParkPricingRepository parkPricingRepository,
        IImageBinaryStorage imageBinaryStorage,
        IParkOpeningHoursRepository? parkOpeningHoursRepository = null,
        ParkOpeningHoursScheduleNormalizer? parkOpeningHoursScheduleNormalizer = null,
        ParkOpeningHoursCoverageSegmentBuilder? parkOpeningHoursCoverageSegmentBuilder = null,
        IHistoryEventRepository? historyEventRepository = null,
        IStandaloneAttractionRepository? standaloneAttractionRepository = null,
        ISocialPublicationService? socialPublicationService = null)
        : this(
            parkRepository,
            parkZoneRepository,
            parkItemRepository,
            parkFounderRepository,
            parkOperatorRepository,
            attractionManufacturerRepository,
            imageRepository,
            remoteImageImporter,
            searchProjectionWriter,
            historyRepository,
            publicSeoUpdateNotifier,
            measurementConversionService,
            parkOpeningHoursRepository,
            parkOpeningHoursScheduleNormalizer,
            parkOpeningHoursCoverageSegmentBuilder,
            historyEventRepository,
            standaloneAttractionRepository,
            socialPublicationService,
            imageBinaryStorage)
    {
        this.parkPricingRepository = parkPricingRepository;
    }

    private async Task ProcessPricingAsync(
        JsonElement root,
        Park targetPark,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (!HasPricingPatch(root))
        {
            return;
        }

        JsonElement? patch = ResolvePricingPatch(root);
        ParkGraphUpsertChange change = BuildEntityChange(
            PricingEntityType,
            targetPark.Id,
            PricingPropertyName,
            string.IsNullOrWhiteSpace(targetPark.Name) ? targetPark.Id : $"{targetPark.Name} pricing",
            "Unchanged",
            PricingPropertyName);

        if (patch is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("pricing doit être un objet JSON.");
            return;
        }

        if (!HasPricingData(patch.Value))
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

        if (this.parkPricingRepository is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("Le traitement des tarifs n'est pas disponible dans ce contexte.");
            return;
        }

        List<string> readErrors = new();
        ParkPricingEntity pricing = ReadPricing(patch.Value, targetPark.Id, readErrors);
        string? requestedParkId = ReadString(patch, "parkId");
        if (!string.IsNullOrWhiteSpace(requestedParkId)
            && !string.Equals(requestedParkId, targetPark.Id, StringComparison.Ordinal))
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

        ParkPricingEntity? existingPricing = await this.parkPricingRepository.GetByParkIdAsync(targetPark.Id, cancellationToken);
        if (existingPricing is not null)
        {
            if (!HasProperty(patch, "historicalSnapshots"))
            {
                pricing.HistoricalSnapshots = existingPricing.HistoricalSnapshots;
            }

            if (!HasProperty(patch, "creditOffers"))
            {
                pricing.CreditOffers = existingPricing.CreditOffers;
            }
        }

        ApplicationResult<ParkPricingEntity> normalizedResult = ParkPricingNormalizer.Normalize(pricing);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            AddPricingValidationErrors(result, normalizedResult);
            return;
        }

        ParkPricingEntity normalizedPricing = normalizedResult.Value;
        bool isNew = existingPricing is null || !ParkPricingNormalizer.HasPublicPricingData(existingPricing);

        AddPricingChanges(change, existingPricing, normalizedPricing);
        if (change.Fields.Count > 0 || isNew)
        {
            change.ChangeType = isNew ? "Created" : "Updated";
        }

        result.Changes.Add(change);
        if (apply)
        {
            await this.parkPricingRepository.UpsertAsync(normalizedPricing, cancellationToken);
        }
    }

    private static bool HasPricingPatch(JsonElement root)
    {
        return (HasProperty(root, PricingPropertyName) && !HasNull(root, PricingPropertyName))
            || (HasProperty(root, LegacyPricingPropertyName) && !HasNull(root, LegacyPricingPropertyName));
    }

    private static JsonElement? ResolvePricingPatch(JsonElement? root)
    {
        return GetObject(root, PricingPropertyName) ?? GetObject(root, LegacyPricingPropertyName);
    }

    private static bool HasPricingData(JsonElement patch)
    {
        return HasNonEmptyPricingArray(patch, "admissionOffers")
            || HasNonEmptyPricingArray(patch, "annualPasses")
            || HasNonEmptyPricingArray(patch, "parkingOffers")
            || HasNonEmptyPricingArray(patch, "creditOffers")
            || HasNonEmptyPricingArray(patch, "historicalSnapshots");
    }

    private static bool HasNonEmptyPricingArray(JsonElement patch, string propertyName)
    {
        if (!patch.TryGetProperty(propertyName, out JsonElement value))
        {
            return false;
        }

        return value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 0;
    }

    private static ParkPricingEntity ReadPricing(JsonElement patch, string targetParkId, List<string> errors)
    {
        return new ParkPricingEntity
        {
            ParkId = targetParkId,
            CurrencyCode = ReadString(patch, "currencyCode") ?? string.Empty,
            SourceUrl = ReadString(patch, "sourceUrl"),
            PurchaseUrl = ReadString(patch, "purchaseUrl"),
            Notes = ReadPricingLocalizedTexts(patch, "notes", "pricing", errors),
            LastVerifiedAtUtc = ReadOptionalPricingUtcDate(patch, "lastVerifiedAtUtc", "pricing", errors),
            AdmissionOffers = ReadAdmissionOffers(patch, "pricing", errors),
            AnnualPasses = ReadAnnualPasses(patch, "pricing", errors),
            ParkingOffers = ReadParkingOffers(patch, "pricing", errors),
            CreditOffers = ReadCreditOffers(patch, "pricing", errors),
            HistoricalSnapshots = ReadHistoricalSnapshots(patch, errors),
        };
    }

    private static List<ParkPricingSnapshot> ReadHistoricalSnapshots(JsonElement patch, List<string> errors)
    {
        JsonElement? array = GetArray(patch, "historicalSnapshots");
        if (array is null)
        {
            if (HasProperty(patch, "historicalSnapshots"))
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

            snapshots.Add(new ParkPricingSnapshot
            {
                Id = ReadString(element, "id"),
                Year = ReadInt(element, "year") ?? 0,
                CurrencyCode = ReadString(element, "currencyCode") ?? string.Empty,
                SourceUrl = ReadString(element, "sourceUrl"),
                Notes = ReadPricingLocalizedTexts(element, "notes", prefix, errors),
                LastVerifiedAtUtc = ReadOptionalPricingUtcDate(element, "lastVerifiedAtUtc", prefix, errors),
                AdmissionOffers = ReadAdmissionOffers(element, prefix, errors),
                AnnualPasses = ReadAnnualPasses(element, prefix, errors),
                ParkingOffers = ReadParkingOffers(element, prefix, errors),
                CreditOffers = ReadCreditOffers(element, prefix, errors),
            });
            index += 1;
        }

        return snapshots;
    }

    private static List<ParkAdmissionPriceOffer> ReadAdmissionOffers(JsonElement patch, string rootPrefix, List<string> errors)
    {
        JsonElement? array = GetArray(patch, "admissionOffers");
        if (array is null)
        {
            if (HasProperty(patch, "admissionOffers"))
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

            offers.Add(new ParkAdmissionPriceOffer
            {
                Id = ReadString(element, "id"),
                Code = ReadString(element, "code") ?? string.Empty,
                AudienceCategory = ReadString(element, "audienceCategory") ?? string.Empty,
                Labels = ReadPricingLocalizedTexts(element, "labels", prefix, errors),
                OnlinePrice = ReadPriceValue(element, "onlinePrice", prefix, errors),
                GatePrice = ReadPriceValue(element, "gatePrice", prefix, errors),
                ValidFrom = ReadOptionalPricingDate(element, "validFrom", prefix, errors),
                ValidTo = ReadOptionalPricingDate(element, "validTo", prefix, errors),
                PurchaseUrl = ReadString(element, "purchaseUrl"),
                Conditions = ReadPricingLocalizedTexts(element, "conditions", prefix, errors),
                SortOrder = ReadInt(element, "sortOrder") ?? index + 1,
            });
            index += 1;
        }

        return offers;
    }

    private static List<ParkAnnualPassOffer> ReadAnnualPasses(JsonElement patch, string rootPrefix, List<string> errors)
    {
        JsonElement? array = GetArray(patch, "annualPasses");
        if (array is null)
        {
            if (HasProperty(patch, "annualPasses"))
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

            offers.Add(new ParkAnnualPassOffer
            {
                Id = ReadString(element, "id"),
                Code = ReadString(element, "code") ?? string.Empty,
                Names = ReadPricingLocalizedTexts(element, "names", prefix, errors),
                OnlinePrice = ReadPriceValue(element, "onlinePrice", prefix, errors),
                GatePrice = ReadPriceValue(element, "gatePrice", prefix, errors),
                ValidFrom = ReadOptionalPricingDate(element, "validFrom", prefix, errors),
                ValidTo = ReadOptionalPricingDate(element, "validTo", prefix, errors),
                PurchaseUrl = ReadString(element, "purchaseUrl"),
                Conditions = ReadPricingLocalizedTexts(element, "conditions", prefix, errors),
                SortOrder = ReadInt(element, "sortOrder") ?? index + 1,
            });
            index += 1;
        }

        return offers;
    }

    private static List<ParkParkingPriceOffer> ReadParkingOffers(JsonElement patch, string rootPrefix, List<string> errors)
    {
        JsonElement? array = GetArray(patch, "parkingOffers");
        if (array is null)
        {
            if (HasProperty(patch, "parkingOffers"))
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

            offers.Add(new ParkParkingPriceOffer
            {
                Id = ReadString(element, "id"),
                Code = ReadString(element, "code") ?? string.Empty,
                Labels = ReadPricingLocalizedTexts(element, "labels", prefix, errors),
                OnlinePrice = ReadPriceValue(element, "onlinePrice", prefix, errors),
                GatePrice = ReadPriceValue(element, "gatePrice", prefix, errors),
                ValidFrom = ReadOptionalPricingDate(element, "validFrom", prefix, errors),
                ValidTo = ReadOptionalPricingDate(element, "validTo", prefix, errors),
                PurchaseUrl = ReadString(element, "purchaseUrl"),
                Conditions = ReadPricingLocalizedTexts(element, "conditions", prefix, errors),
                SortOrder = ReadInt(element, "sortOrder") ?? index + 1,
            });
            index += 1;
        }

        return offers;
    }

    private static List<ParkCreditOffer> ReadCreditOffers(JsonElement patch, string rootPrefix, List<string> errors)
    {
        JsonElement? array = GetArray(patch, "creditOffers");
        if (array is null)
        {
            if (HasProperty(patch, "creditOffers"))
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

            JsonElement? prices = GetObject(element, "prices");
            if (prices is null && HasProperty(element, "prices"))
            {
                errors.Add($"{prefix}.prices doit être un objet.");
            }

            offers.Add(new ParkCreditOffer
            {
                Id = ReadString(element, "id"),
                UnitCode = ReadString(element, "unitCode") ?? string.Empty,
                Quantity = ReadInt(element, "quantity") ?? 0,
                Labels = ReadPricingLocalizedTexts(element, "labels", prefix, errors),
                Prices = new ParkCreditOfferPrices
                {
                    OnlinePrice = ReadOptionalPricingDecimal(prices, "onlinePrice", $"{prefix}.prices", errors),
                    GatePrice = ReadOptionalPricingDecimal(prices, "gatePrice", $"{prefix}.prices", errors),
                },
                ValidFrom = ReadOptionalPricingDate(element, "validFrom", prefix, errors),
                ValidTo = ReadOptionalPricingDate(element, "validTo", prefix, errors),
                PurchaseUrl = ReadString(element, "purchaseUrl"),
                Conditions = ReadPricingLocalizedTexts(element, "conditions", prefix, errors),
                SortOrder = ReadInt(element, "sortOrder") ?? index + 1,
            });
            index += 1;
        }

        return offers;
    }

    private static decimal? ReadOptionalPricingDecimal(
        JsonElement? element,
        string propertyName,
        string prefix,
        List<string> errors)
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

    private static List<AmusementPark.Core.Localization.LocalizedText> ReadPricingLocalizedTexts(
        JsonElement element,
        string propertyName,
        string prefix,
        List<string> errors)
    {
        JsonElement? array = GetArray(element, propertyName);
        if (array is null)
        {
            if (HasProperty(element, propertyName))
            {
                errors.Add($"{prefix}.{propertyName} doit être un tableau.");
            }

            return new List<AmusementPark.Core.Localization.LocalizedText>();
        }

        return ReadLocalizedTexts(array);
    }

    private static ParkPriceValue? ReadPriceValue(JsonElement element, string propertyName, string prefix, List<string> errors)
    {
        if (!HasProperty(element, propertyName) || HasNull(element, propertyName))
        {
            return null;
        }

        JsonElement? priceElement = GetObject(element, propertyName);
        if (priceElement is null)
        {
            errors.Add($"{prefix}.{propertyName} doit être un objet.");
            return null;
        }

        string fieldPrefix = $"{prefix}.{propertyName}";
        string? modeValue = ReadString(priceElement, "mode");
        ParkPricingMode mode = ParkPricingMode.Fixed;
        if (string.IsNullOrWhiteSpace(modeValue) || !Enum.TryParse(modeValue, true, out mode) || !Enum.IsDefined(mode))
        {
            errors.Add($"{fieldPrefix}.mode doit valoir Fixed, Range ou Dynamic.");
        }

        return new ParkPriceValue
        {
            Mode = mode,
            Amount = ReadPricingDecimal(priceElement.Value, "amount", $"{fieldPrefix}.amount", errors),
            MinimumAmount = ReadPricingDecimal(priceElement.Value, "minimumAmount", $"{fieldPrefix}.minimumAmount", errors),
            MaximumAmount = ReadPricingDecimal(priceElement.Value, "maximumAmount", $"{fieldPrefix}.maximumAmount", errors),
        };
    }

    private static decimal? ReadPricingDecimal(JsonElement element, string propertyName, string fieldPath, List<string> errors)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal numericValue))
        {
            return numericValue;
        }

        if (value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal stringValue))
        {
            return stringValue;
        }

        errors.Add($"{fieldPath} doit être un nombre décimal.");
        return null;
    }

    private static DateOnly? ReadOptionalPricingDate(JsonElement element, string propertyName, string prefix, List<string> errors)
    {
        if (!HasProperty(element, propertyName) || HasNull(element, propertyName))
        {
            return null;
        }

        string? value = ReadString(element, propertyName);
        if (DateOnly.TryParseExact(value, PricingDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            return date;
        }

        errors.Add($"{prefix}.{propertyName} doit utiliser le format {PricingDateFormat}.");
        return null;
    }

    private static DateTime? ReadOptionalPricingUtcDate(JsonElement element, string propertyName, string prefix, List<string> errors)
    {
        if (!HasProperty(element, propertyName) || HasNull(element, propertyName))
        {
            return null;
        }

        string? value = ReadString(element, propertyName);
        if (DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset parsed))
        {
            return parsed.UtcDateTime;
        }

        errors.Add($"{prefix}.{propertyName} doit être une date ISO 8601 valide.");
        return null;
    }

    private static void AddPricingValidationErrors(ParkGraphUpsertResult result, ApplicationResult<ParkPricingEntity> normalizedResult)
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

    private static void AddPricingChanges(ParkGraphUpsertChange change, ParkPricingEntity? existingPricing, ParkPricingEntity normalizedPricing)
    {
        AddChange(change, "pricing.currencyCode", existingPricing?.CurrencyCode, normalizedPricing.CurrencyCode);
        AddChange(change, "pricing.sourceUrl", existingPricing?.SourceUrl, normalizedPricing.SourceUrl);
        AddChange(change, "pricing.purchaseUrl", existingPricing?.PurchaseUrl, normalizedPricing.PurchaseUrl);
        AddChange(change, "pricing.notes", DescribePricing(existingPricing?.Notes), DescribePricing(normalizedPricing.Notes));
        AddChange(change, "pricing.lastVerifiedAtUtc", existingPricing?.LastVerifiedAtUtc, normalizedPricing.LastVerifiedAtUtc);
        AddChange(change, "pricing.admissionOffers", DescribePricing(existingPricing?.AdmissionOffers), DescribePricing(normalizedPricing.AdmissionOffers));
        AddChange(change, "pricing.annualPasses", DescribePricing(existingPricing?.AnnualPasses), DescribePricing(normalizedPricing.AnnualPasses));
        AddChange(change, "pricing.parkingOffers", DescribePricing(existingPricing?.ParkingOffers), DescribePricing(normalizedPricing.ParkingOffers));
        AddChange(change, "pricing.creditOffers", DescribePricing(existingPricing?.CreditOffers), DescribePricing(normalizedPricing.CreditOffers));
        AddChange(change, "pricing.historicalSnapshots", DescribePricing(existingPricing?.HistoricalSnapshots), DescribePricing(normalizedPricing.HistoricalSnapshots));
    }

    private static string DescribePricing<T>(IReadOnlyCollection<T>? values)
    {
        return values is null || values.Count == 0
            ? string.Empty
            : JsonSerializer.Serialize(values);
    }
}
