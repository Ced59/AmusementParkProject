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
internal static class ParkGraphUpsertProcessorHistoryExtensions
{
    internal static async Task ProcessHistoryEventsAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, Park targetPark, Dictionary<string, string> itemKeys, Dictionary<string, string> imageKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        JsonElement? events = ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryEvents(root);
        if (events is null)
        {
            return;
        }

        if (processorContext.historyEventRepository is null)
        {
            result.Warnings.Add("La section history est ignoree car le repository d'historique n'est pas disponible.");
            return;
        }

        foreach (JsonElement patch in events.Value.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key"));
            string? eventType = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "eventType") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "type"));
            if (string.IsNullOrWhiteSpace(eventType))
            {
                result.Errors.Add("Un evenement history doit definir eventType.");
                continue;
            }

            HistoryEntityType entityType = ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryEntityType(patch);
            string? ownerId = ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryOwnerId(patch, entityType, targetPark, itemKeys);
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                result.Errors.Add($"Impossible de resoudre le proprietaire de l'evenement history '{key ?? eventType}'.");
                continue;
            }

            if (!ParkGraphUpsertProcessorHistoryExtensions.IsValidHistoryEventType(entityType, eventType))
            {
                result.Errors.Add($"Le type d'evenement history '{eventType}' n'est pas valide pour '{entityType}'.");
                continue;
            }

            HistoryDateParts? dateParts = ParkGraphUpsertProcessorHistoryExtensions.ReadHistoryDate(patch);
            if (dateParts is null)
            {
                result.Errors.Add($"L'evenement history '{key ?? eventType}' doit definir une date valide.");
                continue;
            }

            key ??= ParkGraphUpsertProcessorHistoryExtensions.BuildHistoryKey(entityType, ownerId, eventType, dateParts);
            HistoryEvent? existing = await processorContext.historyEventRepository.GetByOwnerKeyAsync(entityType, ownerId, key, cancellationToken);
            HistoryEvent historyEvent = existing ?? new HistoryEvent();
            ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("HistoryEvent", historyEvent.Id, key, ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryDisplayName(patch, eventType), existing is null ? "Created" : "Unchanged", existing is null ? "key" : "ownerKey");
            ParkGraphUpsertProcessorHistoryExtensions.PatchHistoryEvent(historyEvent, patch, targetPark.Id, entityType, ownerId, key, eventType, dateParts, imageKeys, result, apply, change);
            if (change.Fields.Count > 0 || existing is null)
            {
                change.ChangeType = existing is null ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || existing is null))
            {
                historyEvent = existing is null ? await processorContext.historyEventRepository.CreateAsync(historyEvent, cancellationToken) : await processorContext.historyEventRepository.UpdateAsync(historyEvent.Id, historyEvent, cancellationToken) ?? historyEvent;
                change.EntityId = historyEvent.Id;
            }

            result.Changes.Add(change);
        }
    }

    internal static JsonElement? ResolveHistoryEvents(JsonElement root)
    {
        JsonElement? history = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "history");
        return ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(history, "events") ?? ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(root, "historyEvents");
    }

    internal static HistoryEntityType ResolveHistoryEntityType(JsonElement patch)
    {
        string? owner = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "owner") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "entityType") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "target"));
        if (string.Equals(owner, "parkItem", StringComparison.OrdinalIgnoreCase) || string.Equals(owner, "item", StringComparison.OrdinalIgnoreCase) || string.Equals(owner, "attraction", StringComparison.OrdinalIgnoreCase))
        {
            return HistoryEntityType.ParkItem;
        }

        return ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnum(patch, "entityType", HistoryEntityType.Park);
    }

    internal static string? ResolveHistoryOwnerId(JsonElement patch, HistoryEntityType entityType, Park targetPark, Dictionary<string, string> itemKeys)
    {
        if (entityType == HistoryEntityType.Park)
        {
            return ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "parkId")) ?? targetPark.Id;
        }

        string? ownerId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "parkItemId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "itemId"));
        if (!string.IsNullOrWhiteSpace(ownerId))
        {
            return ownerId;
        }

        string? itemKey = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "itemKey") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "parkItemKey"));
        if (!string.IsNullOrWhiteSpace(itemKey) && itemKeys.TryGetValue(itemKey, out string? resolvedItemId))
        {
            return resolvedItemId;
        }

        if (!string.IsNullOrWhiteSpace(itemKey) && itemKeys.TryGetValue($"item:{ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(itemKey)}", out string? resolvedByName))
        {
            return resolvedByName;
        }

        return null;
    }

    internal static bool IsValidHistoryEventType(HistoryEntityType entityType, string eventType)
    {
        return entityType == HistoryEntityType.Park ? ParkGraphUpsertProcessorJsonReadingExtensions.TryReadEnum(eventType, out ParkHistoryEventType _) : ParkGraphUpsertProcessorJsonReadingExtensions.TryReadEnum(eventType, out ParkItemHistoryEventType _);
    }

    internal static void PatchHistoryEvent(HistoryEvent historyEvent, JsonElement patch, string? defaultContextParkId, HistoryEntityType entityType, string ownerId, string key, string eventType, HistoryDateParts dateParts, Dictionary<string, string> imageKeys, ParkGraphUpsertResult result, bool apply, ParkGraphUpsertChange change)
    {
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "key", historyEvent.Key, key);
        historyEvent.Key = key;
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "entityType", historyEvent.EntityType, entityType);
        historyEvent.EntityType = entityType;
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "ownerId", historyEvent.OwnerId, ownerId);
        historyEvent.OwnerId = ownerId;
        string? parkId = entityType == HistoryEntityType.Park ? ownerId : ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "parkId"));
        string? parkItemId = entityType == HistoryEntityType.ParkItem ? ownerId : ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "parkItemId"));
        string? contextParkId = ParkGraphUpsertProcessorHistoryExtensions.ReadHistoryContextParkId(patch);
        if (entityType == HistoryEntityType.ParkItem && !ParkGraphUpsertProcessorHistoryExtensions.HasExplicitHistoryContextParkProperty(patch))
        {
            contextParkId ??= parkId ?? defaultContextParkId;
        }

        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "parkId", historyEvent.ParkId, parkId);
        historyEvent.ParkId = parkId;
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "parkItemId", historyEvent.ParkItemId, parkItemId);
        historyEvent.ParkItemId = parkItemId;
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "contextParkId", historyEvent.ContextParkId, contextParkId);
        historyEvent.ContextParkId = contextParkId;
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "year", historyEvent.Year, dateParts.Year);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "month", historyEvent.Month, dateParts.Month);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "day", historyEvent.Day, dateParts.Day);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "datePrecision", historyEvent.DatePrecision, dateParts.Precision);
        historyEvent.Year = dateParts.Year;
        historyEvent.Month = dateParts.Precision == HistoryDatePrecision.Year ? null : dateParts.Month;
        historyEvent.Day = dateParts.Precision == HistoryDatePrecision.Day ? dateParts.Day : null;
        historyEvent.DatePrecision = dateParts.Precision;
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "eventType", historyEvent.EventType, eventType);
        historyEvent.EventType = eventType;
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isMajor", historyEvent.IsMajor, value => historyEvent.IsMajor = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isVisible", historyEvent.IsVisible, value => historyEvent.IsVisible = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "slug", historyEvent.Slug, value => historyEvent.Slug = value, change);
        if (ParkGraphUpsertProcessorImageKeysExtensions.TryReadHistoryImageIdPatch(patch, "mainImageId", "mainImageKey", "imageKey", imageKeys, result, apply, $"l'evenement history '{key}'", out string? mainImageId))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "mainImageId", historyEvent.MainImageId, mainImageId);
            historyEvent.MainImageId = mainImageId;
        }

        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "previousName", historyEvent.PreviousName, value => historyEvent.PreviousName = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "newName", historyEvent.NewName, value => historyEvent.NewName = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "previousLogoImageId", historyEvent.PreviousLogoImageId, value => historyEvent.PreviousLogoImageId = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "newLogoImageId", historyEvent.NewLogoImageId, value => historyEvent.NewLogoImageId = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "previousOperatorId", historyEvent.PreviousOperatorId, value => historyEvent.PreviousOperatorId = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "newOperatorId", historyEvent.NewOperatorId, value => historyEvent.NewOperatorId = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "locationLabel", historyEvent.LocationLabel, value => historyEvent.LocationLabel = value, change);
        List<LocalizedText> titles = ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexible(patch, "titles", "title");
        if (titles.Count > 0)
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "titles", ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(historyEvent.Titles), ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(titles));
            historyEvent.Titles = titles;
        }

        List<LocalizedText> summaries = ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexible(patch, "summaries", "summary");
        if (summaries.Count > 0)
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "summaries", ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(historyEvent.Summaries), ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(summaries));
            historyEvent.Summaries = summaries;
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "relatedParkIds"))
        {
            List<string> relatedParkIds = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadStringArray(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "relatedParkIds"));
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "relatedParkIds", ParkGraphUpsertProcessorImagesExtensions.DescribeStringCollection(historyEvent.RelatedParkIds), ParkGraphUpsertProcessorImagesExtensions.DescribeStringCollection(relatedParkIds));
            historyEvent.RelatedParkIds = relatedParkIds;
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "relatedParkItemIds"))
        {
            List<string> relatedParkItemIds = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadStringArray(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "relatedParkItemIds"));
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "relatedParkItemIds", ParkGraphUpsertProcessorImagesExtensions.DescribeStringCollection(historyEvent.RelatedParkItemIds), ParkGraphUpsertProcessorImagesExtensions.DescribeStringCollection(relatedParkItemIds));
            historyEvent.RelatedParkItemIds = relatedParkItemIds;
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "sources"))
        {
            List<HistorySourceReference> sources = ParkGraphUpsertProcessorHistoryExtensions.ReadHistorySources(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "sources"));
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "sources", ParkGraphUpsertProcessorHistoryComparisonExtensions.DescribeHistorySourcesForDiff(historyEvent.Sources), ParkGraphUpsertProcessorHistoryComparisonExtensions.DescribeHistorySourcesForDiff(sources));
            historyEvent.Sources = sources;
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "article"))
        {
            HistoryArticle? previousArticle = historyEvent.Article;
            HistoryArticle? updatedArticle = ParkGraphUpsertProcessorHistoryExtensions.ReadHistoryArticle(ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "article"), imageKeys, result, apply, key, previousArticle);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "article", ParkGraphUpsertProcessorHistoryComparisonExtensions.DescribeHistoryArticleForDiff(previousArticle), ParkGraphUpsertProcessorHistoryComparisonExtensions.DescribeHistoryArticleForDiff(updatedArticle));
            historyEvent.Article = updatedArticle;
            if (updatedArticle is not null)
            {
                historyEvent.IsMajor = true;
            }
        }
    }

    internal static bool HasExplicitHistoryContextParkProperty(JsonElement patch)
    {
        return ParkGraphUpsertProcessorHistoryExtensions.HasExplicitHistoryContextParkValue(patch, "contextParkId") || ParkGraphUpsertProcessorHistoryExtensions.HasExplicitHistoryContextParkValue(patch, "contextPark") || ParkGraphUpsertProcessorHistoryExtensions.HasExplicitHistoryContextParkValue(patch, "parkContextId");
    }

    internal static bool HasExplicitHistoryContextParkValue(JsonElement patch, string propertyName)
    {
        if (patch.ValueKind != JsonValueKind.Object || !patch.TryGetProperty(propertyName, out JsonElement property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return false;
        }

        string? rawValue = property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
        return ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(rawValue)is not null;
    }

    internal static string? ReadHistoryContextParkId(JsonElement patch)
    {
        string? contextParkId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "contextParkId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "contextPark") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "parkContextId"));
        return ParkGraphUpsertProcessorHistoryExtensions.IsExternalHistoryContextMarker(contextParkId) ? null : contextParkId;
    }

    internal static bool IsExternalHistoryContextMarker(string? contextParkId)
    {
        return string.Equals(contextParkId, "external", StringComparison.OrdinalIgnoreCase) || string.Equals(contextParkId, "outside", StringComparison.OrdinalIgnoreCase) || string.Equals(contextParkId, "none", StringComparison.OrdinalIgnoreCase) || string.Equals(contextParkId, "null", StringComparison.OrdinalIgnoreCase);
    }

    internal static HistoryDateParts? ReadHistoryDate(JsonElement patch)
    {
        string? date = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "date"));
        if (!string.IsNullOrWhiteSpace(date))
        {
            string[] parts = date.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 1 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
            {
                int? month = parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedMonth) ? parsedMonth : null;
                int? day = parts.Length >= 3 && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedDay) ? parsedDay : null;
                HistoryDatePrecision precision = day.HasValue ? HistoryDatePrecision.Day : month.HasValue ? HistoryDatePrecision.Month : HistoryDatePrecision.Year;
                return ParkGraphUpsertProcessorHistoryExtensions.IsValidHistoryDate(year, month, day, precision) ? new HistoryDateParts(year, month, day, precision) : null;
            }
        }

        int? explicitYear = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(patch, "year");
        if (!explicitYear.HasValue)
        {
            return null;
        }

        int? explicitMonth = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(patch, "month");
        int? explicitDay = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(patch, "day");
        HistoryDatePrecision explicitPrecision = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnum(patch, "datePrecision", ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnum(patch, "precision", explicitDay.HasValue ? HistoryDatePrecision.Day : explicitMonth.HasValue ? HistoryDatePrecision.Month : HistoryDatePrecision.Year));
        return ParkGraphUpsertProcessorHistoryExtensions.IsValidHistoryDate(explicitYear.Value, explicitMonth, explicitDay, explicitPrecision) ? new HistoryDateParts(explicitYear.Value, explicitMonth, explicitDay, explicitPrecision) : null;
    }

    internal static bool IsValidHistoryDate(int year, int? month, int? day, HistoryDatePrecision precision)
    {
        if (year <= 0 || month is < 1 or > 12 || day is < 1 or > 31)
        {
            return false;
        }

        return precision switch
        {
            HistoryDatePrecision.Year => true,
            HistoryDatePrecision.Month => month.HasValue,
            HistoryDatePrecision.Day => month.HasValue && day.HasValue,
            _ => false,
        };
    }

    internal static List<LocalizedText> ReadLocalizedTextsFlexible(JsonElement element, string arrayPropertyName, string compactPropertyName)
    {
        if (element.TryGetProperty(arrayPropertyName, out JsonElement pluralValue))
        {
            List<LocalizedText> pluralValues = ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexibleValue(pluralValue);
            if (pluralValues.Count > 0)
            {
                return pluralValues;
            }
        }

        if (!element.TryGetProperty(compactPropertyName, out JsonElement compact))
        {
            return new List<LocalizedText>();
        }

        return ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexibleValue(compact);
    }

    internal static List<LocalizedText> ReadLocalizedTextsFlexibleValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            return ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(value);
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            string? text = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(value.GetString());
            return string.IsNullOrWhiteSpace(text) ? new List<LocalizedText>() : new List<LocalizedText>
            {
                new LocalizedText("fr", text)
            };
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return new List<LocalizedText>();
        }

        List<LocalizedText> values = new List<LocalizedText>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? text = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(property.Value.GetString());
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(property.Name))
            {
                values.Add(new LocalizedText(property.Name.Trim().ToLowerInvariant(), text));
            }
        }

        return values;
    }

    internal static HistoryArticle? ReadHistoryArticle(JsonElement? element, Dictionary<string, string> imageKeys, ParkGraphUpsertResult result, bool apply, string eventKey, HistoryArticle? previousArticle)
    {
        if (element is null)
        {
            return null;
        }

        string? mainImageId = null;
        if (ParkGraphUpsertProcessorImageKeysExtensions.TryReadHistoryImageIdPatch(element.Value, "mainImageId", "mainImageKey", "imageKey", imageKeys, result, apply, $"l'article history '{eventKey}'", out string? resolvedMainImageId))
        {
            mainImageId = resolvedMainImageId;
        }
        else if (apply && ParkGraphUpsertProcessorImageKeysExtensions.HasHistoryImageIdPatch(element.Value, "mainImageId", "mainImageKey", "imageKey"))
        {
            mainImageId = previousArticle?.MainImageId;
        }

        return new HistoryArticle
        {
            Slug = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, "slug")),
            Titles = ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexible(element.Value, "titles", "title"),
            Subtitles = ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexible(element.Value, "subtitles", "subtitle"),
            Summaries = ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexible(element.Value, "summaries", "summary"),
            MainImageId = mainImageId,
            Blocks = ParkGraphUpsertProcessorHistoryExtensions.ReadHistoryArticleBlocks(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(element, "blocks"), imageKeys, result, apply, eventKey, previousArticle is null ? Array.Empty<HistoryArticleBlock>() : previousArticle.Blocks),
            Sources = ParkGraphUpsertProcessorHistoryExtensions.ReadHistorySources(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(element, "sources")),
            IsPublished = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(element, "isPublished") ?? true,
        };
    }

    internal static List<HistoryArticleBlock> ReadHistoryArticleBlocks(JsonElement? array, Dictionary<string, string> imageKeys, ParkGraphUpsertResult result, bool apply, string eventKey, IReadOnlyList<HistoryArticleBlock> previousBlocks)
    {
        if (array is null)
        {
            return new List<HistoryArticleBlock>();
        }

        List<HistoryArticleBlock> blocks = new List<HistoryArticleBlock>();
        HashSet<HistoryArticleBlock> matchedPreviousBlocks = new HashSet<HistoryArticleBlock>();
        int fallbackSortOrder = 0;
        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            fallbackSortOrder++;
            int sortOrder = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(item, "sortOrder") ?? fallbackSortOrder;
            string? requestedBlockId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "id"));
            HistoryArticleBlock? previousBlock = ParkGraphUpsertProcessorHistoryExtensions.FindPreviousHistoryArticleBlock(previousBlocks, matchedPreviousBlocks, requestedBlockId, sortOrder, fallbackSortOrder - 1);
            if (previousBlock is not null)
            {
                matchedPreviousBlocks.Add(previousBlock);
            }

            string blockId = requestedBlockId ?? ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(previousBlock?.Id) ?? Guid.NewGuid().ToString("N");
            string? imageId = null;
            if (ParkGraphUpsertProcessorImageKeysExtensions.TryReadHistoryImageIdPatch(item, "imageId", "imageKey", "mainImageKey", imageKeys, result, apply, $"le bloc '{blockId}' de l'article history '{eventKey}'", out string? resolvedImageId))
            {
                imageId = resolvedImageId;
            }
            else if (apply && ParkGraphUpsertProcessorImageKeysExtensions.HasHistoryImageIdPatch(item, "imageId", "imageKey", "mainImageKey"))
            {
                imageId = previousBlock?.ImageId;
            }

            HistoryArticleBlock block = new HistoryArticleBlock
            {
                Id = blockId,
                Type = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnum(item, "type", HistoryArticleBlockType.Paragraph),
                SortOrder = sortOrder,
                HeadingLevel = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(item, "headingLevel"),
                Texts = ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexible(item, "texts", "text"),
                ImageId = imageId,
                ImageIds = ParkGraphUpsertProcessorImageKeysExtensions.ReadHistoryImageIds(item, imageKeys, result, apply, $"le bloc '{blockId}' de l'article history '{eventKey}'", previousBlock is null ? Array.Empty<string>() : previousBlock.ImageIds),
                Captions = ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexible(item, "captions", "caption"),
            };
            blocks.Add(block);
        }

        return blocks;
    }

    internal static HistoryArticleBlock? FindPreviousHistoryArticleBlock(IReadOnlyList<HistoryArticleBlock> previousBlocks, IReadOnlySet<HistoryArticleBlock> matchedPreviousBlocks, string? requestedBlockId, int sortOrder, int blockIndex)
    {
        if (!string.IsNullOrWhiteSpace(requestedBlockId))
        {
            return previousBlocks.FirstOrDefault(block => !matchedPreviousBlocks.Contains(block) && string.Equals(ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(block.Id), requestedBlockId, StringComparison.Ordinal));
        }

        HistoryArticleBlock? sameSortOrder = previousBlocks.FirstOrDefault(block => !matchedPreviousBlocks.Contains(block) && block.SortOrder == sortOrder);
        if (sameSortOrder is not null)
        {
            return sameSortOrder;
        }

        if (blockIndex >= 0 && blockIndex < previousBlocks.Count && !matchedPreviousBlocks.Contains(previousBlocks[blockIndex]))
        {
            return previousBlocks[blockIndex];
        }

        return previousBlocks.FirstOrDefault(block => !matchedPreviousBlocks.Contains(block));
    }

    internal static List<HistorySourceReference> ReadHistorySources(JsonElement? array)
    {
        if (array is null)
        {
            return new List<HistorySourceReference>();
        }

        List<HistorySourceReference> sources = new List<HistorySourceReference>();
        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? url = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "url"));
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            sources.Add(new HistorySourceReference { Label = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "label")), Url = url, AccessedAt = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "accessedAt")), });
        }

        return sources;
    }

    internal static string BuildHistoryKey(HistoryEntityType entityType, string ownerId, string eventType, HistoryDateParts dateParts)
    {
        string month = dateParts.Month.HasValue ? dateParts.Month.Value.ToString("00", CultureInfo.InvariantCulture) : "00";
        string day = dateParts.Day.HasValue ? dateParts.Day.Value.ToString("00", CultureInfo.InvariantCulture) : "00";
        return $"{entityType}-{ownerId}-{dateParts.Year.ToString(CultureInfo.InvariantCulture)}-{month}-{day}-{ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(eventType)}";
    }

    internal static string ResolveHistoryDisplayName(JsonElement patch, string fallback)
    {
        List<LocalizedText> titles = ParkGraphUpsertProcessorHistoryExtensions.ReadLocalizedTextsFlexible(patch, "titles", "title");
        return titles.FirstOrDefault(static title => !string.IsNullOrWhiteSpace(title.Value))?.Value ?? fallback;
    }
}
