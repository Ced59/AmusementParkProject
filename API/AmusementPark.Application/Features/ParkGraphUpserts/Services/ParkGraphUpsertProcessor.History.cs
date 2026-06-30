using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task ProcessHistoryEventsAsync(
        JsonElement root,
        Park targetPark,
        Dictionary<string, string> itemKeys,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        JsonElement? events = ResolveHistoryEvents(root);
        if (events is null)
        {
            return;
        }

        if (this.historyEventRepository is null)
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

            string? key = NormalizeString(ReadString(patch, "key"));
            string? eventType = NormalizeString(ReadString(patch, "eventType") ?? ReadString(patch, "type"));
            if (string.IsNullOrWhiteSpace(eventType))
            {
                result.Errors.Add("Un evenement history doit definir eventType.");
                continue;
            }

            HistoryEntityType entityType = ResolveHistoryEntityType(patch);
            string? ownerId = ResolveHistoryOwnerId(patch, entityType, targetPark, itemKeys);
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                result.Errors.Add($"Impossible de resoudre le proprietaire de l'evenement history '{key ?? eventType}'.");
                continue;
            }

            if (!IsValidHistoryEventType(entityType, eventType))
            {
                result.Errors.Add($"Le type d'evenement history '{eventType}' n'est pas valide pour '{entityType}'.");
                continue;
            }

            HistoryDateParts? dateParts = ReadHistoryDate(patch);
            if (dateParts is null)
            {
                result.Errors.Add($"L'evenement history '{key ?? eventType}' doit definir une date valide.");
                continue;
            }

            key ??= BuildHistoryKey(entityType, ownerId, eventType, dateParts);
            HistoryEvent? existing = await this.historyEventRepository.GetByOwnerKeyAsync(entityType, ownerId, key, cancellationToken);
            HistoryEvent historyEvent = existing ?? new HistoryEvent();
            ParkGraphUpsertChange change = BuildEntityChange(
                "HistoryEvent",
                historyEvent.Id,
                key,
                ResolveHistoryDisplayName(patch, eventType),
                existing is null ? "Created" : "Unchanged",
                existing is null ? "key" : "ownerKey");

            PatchHistoryEvent(historyEvent, patch, targetPark, entityType, ownerId, key, eventType, dateParts, change);

            if (change.Fields.Count > 0 || existing is null)
            {
                change.ChangeType = existing is null ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || existing is null))
            {
                historyEvent = existing is null
                    ? await this.historyEventRepository.CreateAsync(historyEvent, cancellationToken)
                    : await this.historyEventRepository.UpdateAsync(historyEvent.Id, historyEvent, cancellationToken) ?? historyEvent;
                change.EntityId = historyEvent.Id;
            }

            result.Changes.Add(change);
        }
    }

    private static JsonElement? ResolveHistoryEvents(JsonElement root)
    {
        JsonElement? history = GetObject(root, "history");
        return GetArray(history, "events") ?? GetArray(root, "historyEvents");
    }

    private static HistoryEntityType ResolveHistoryEntityType(JsonElement patch)
    {
        string? owner = NormalizeString(ReadString(patch, "owner") ?? ReadString(patch, "entityType") ?? ReadString(patch, "target"));
        if (string.Equals(owner, "parkItem", StringComparison.OrdinalIgnoreCase)
            || string.Equals(owner, "item", StringComparison.OrdinalIgnoreCase)
            || string.Equals(owner, "attraction", StringComparison.OrdinalIgnoreCase))
        {
            return HistoryEntityType.ParkItem;
        }

        return ReadEnum(patch, "entityType", HistoryEntityType.Park);
    }

    private static string? ResolveHistoryOwnerId(JsonElement patch, HistoryEntityType entityType, Park targetPark, Dictionary<string, string> itemKeys)
    {
        if (entityType == HistoryEntityType.Park)
        {
            return NormalizeString(ReadString(patch, "ownerId") ?? ReadString(patch, "parkId")) ?? targetPark.Id;
        }

        string? ownerId = NormalizeString(ReadString(patch, "ownerId") ?? ReadString(patch, "parkItemId") ?? ReadString(patch, "itemId"));
        if (!string.IsNullOrWhiteSpace(ownerId))
        {
            return ownerId;
        }

        string? itemKey = NormalizeString(ReadString(patch, "itemKey") ?? ReadString(patch, "parkItemKey"));
        if (!string.IsNullOrWhiteSpace(itemKey) && itemKeys.TryGetValue(itemKey, out string? resolvedItemId))
        {
            return resolvedItemId;
        }

        if (!string.IsNullOrWhiteSpace(itemKey) && itemKeys.TryGetValue($"item:{NormalizeKey(itemKey)}", out string? resolvedByName))
        {
            return resolvedByName;
        }

        return null;
    }

    private static bool IsValidHistoryEventType(HistoryEntityType entityType, string eventType)
    {
        return entityType == HistoryEntityType.Park
            ? TryReadEnum(eventType, out ParkHistoryEventType _)
            : TryReadEnum(eventType, out ParkItemHistoryEventType _);
    }

    private static void PatchHistoryEvent(
        HistoryEvent historyEvent,
        JsonElement patch,
        Park targetPark,
        HistoryEntityType entityType,
        string ownerId,
        string key,
        string eventType,
        HistoryDateParts dateParts,
        ParkGraphUpsertChange change)
    {
        AddChange(change, "key", historyEvent.Key, key);
        historyEvent.Key = key;
        AddChange(change, "entityType", historyEvent.EntityType, entityType);
        historyEvent.EntityType = entityType;
        AddChange(change, "ownerId", historyEvent.OwnerId, ownerId);
        historyEvent.OwnerId = ownerId;

        string? parkId = entityType == HistoryEntityType.Park ? ownerId : NormalizeString(ReadString(patch, "parkId"));
        string? parkItemId = entityType == HistoryEntityType.ParkItem ? ownerId : NormalizeString(ReadString(patch, "parkItemId"));
        string? contextParkId = ReadHistoryContextParkId(patch);
        if (entityType == HistoryEntityType.ParkItem && !HasHistoryContextParkProperty(patch))
        {
            contextParkId ??= parkId ?? targetPark.Id;
        }

        AddChange(change, "parkId", historyEvent.ParkId, parkId);
        historyEvent.ParkId = parkId;
        AddChange(change, "parkItemId", historyEvent.ParkItemId, parkItemId);
        historyEvent.ParkItemId = parkItemId;
        AddChange(change, "contextParkId", historyEvent.ContextParkId, contextParkId);
        historyEvent.ContextParkId = contextParkId;

        AddChange(change, "year", historyEvent.Year, dateParts.Year);
        AddChange(change, "month", historyEvent.Month, dateParts.Month);
        AddChange(change, "day", historyEvent.Day, dateParts.Day);
        AddChange(change, "datePrecision", historyEvent.DatePrecision, dateParts.Precision);
        historyEvent.Year = dateParts.Year;
        historyEvent.Month = dateParts.Precision == HistoryDatePrecision.Year ? null : dateParts.Month;
        historyEvent.Day = dateParts.Precision == HistoryDatePrecision.Day ? dateParts.Day : null;
        historyEvent.DatePrecision = dateParts.Precision;

        AddChange(change, "eventType", historyEvent.EventType, eventType);
        historyEvent.EventType = eventType;
        PatchBool(patch, "isMajor", historyEvent.IsMajor, value => historyEvent.IsMajor = value, change);
        PatchBool(patch, "isVisible", historyEvent.IsVisible, value => historyEvent.IsVisible = value, change);
        PatchString(patch, "slug", historyEvent.Slug, value => historyEvent.Slug = value, change);
        PatchString(patch, "mainImageId", historyEvent.MainImageId, value => historyEvent.MainImageId = value, change);
        PatchString(patch, "previousName", historyEvent.PreviousName, value => historyEvent.PreviousName = value, change);
        PatchString(patch, "newName", historyEvent.NewName, value => historyEvent.NewName = value, change);
        PatchString(patch, "previousLogoImageId", historyEvent.PreviousLogoImageId, value => historyEvent.PreviousLogoImageId = value, change);
        PatchString(patch, "newLogoImageId", historyEvent.NewLogoImageId, value => historyEvent.NewLogoImageId = value, change);
        PatchString(patch, "previousOperatorId", historyEvent.PreviousOperatorId, value => historyEvent.PreviousOperatorId = value, change);
        PatchString(patch, "newOperatorId", historyEvent.NewOperatorId, value => historyEvent.NewOperatorId = value, change);
        PatchString(patch, "locationLabel", historyEvent.LocationLabel, value => historyEvent.LocationLabel = value, change);

        List<LocalizedText> titles = ReadLocalizedTextsFlexible(patch, "titles", "title");
        if (titles.Count > 0)
        {
            AddChange(change, "titles", DescribeLocalizedTextsForDiff(historyEvent.Titles), DescribeLocalizedTextsForDiff(titles));
            historyEvent.Titles = titles;
        }

        List<LocalizedText> summaries = ReadLocalizedTextsFlexible(patch, "summaries", "summary");
        if (summaries.Count > 0)
        {
            AddChange(change, "summaries", DescribeLocalizedTextsForDiff(historyEvent.Summaries), DescribeLocalizedTextsForDiff(summaries));
            historyEvent.Summaries = summaries;
        }

        if (HasProperty(patch, "relatedParkIds"))
        {
            List<string> relatedParkIds = ReadStringArray(GetArray(patch, "relatedParkIds"));
            AddChange(change, "relatedParkIds", DescribeStringCollection(historyEvent.RelatedParkIds), DescribeStringCollection(relatedParkIds));
            historyEvent.RelatedParkIds = relatedParkIds;
        }

        if (HasProperty(patch, "relatedParkItemIds"))
        {
            List<string> relatedParkItemIds = ReadStringArray(GetArray(patch, "relatedParkItemIds"));
            AddChange(change, "relatedParkItemIds", DescribeStringCollection(historyEvent.RelatedParkItemIds), DescribeStringCollection(relatedParkItemIds));
            historyEvent.RelatedParkItemIds = relatedParkItemIds;
        }

        if (HasProperty(patch, "sources"))
        {
            List<HistorySourceReference> sources = ReadHistorySources(GetArray(patch, "sources"));
            AddChange(change, "sources", historyEvent.Sources.Count, sources.Count);
            historyEvent.Sources = sources;
        }

        if (HasProperty(patch, "article"))
        {
            HistoryArticle? article = ReadHistoryArticle(GetObject(patch, "article"));
            AddChange(change, "article", historyEvent.Article is null ? null : "present", article is null ? null : "present");
            historyEvent.Article = article;
            if (article is not null)
            {
                historyEvent.IsMajor = true;
            }
        }
    }

    private static bool HasHistoryContextParkProperty(JsonElement patch)
    {
        return HasProperty(patch, "contextParkId")
            || HasProperty(patch, "contextPark")
            || HasProperty(patch, "parkContextId");
    }

    private static string? ReadHistoryContextParkId(JsonElement patch)
    {
        string? contextParkId = NormalizeString(ReadString(patch, "contextParkId") ?? ReadString(patch, "contextPark") ?? ReadString(patch, "parkContextId"));
        return IsExternalHistoryContextMarker(contextParkId) ? null : contextParkId;
    }

    private static bool IsExternalHistoryContextMarker(string? contextParkId)
    {
        return string.Equals(contextParkId, "external", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contextParkId, "outside", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contextParkId, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contextParkId, "null", StringComparison.OrdinalIgnoreCase);
    }

    private static HistoryDateParts? ReadHistoryDate(JsonElement patch)
    {
        string? date = NormalizeString(ReadString(patch, "date"));
        if (!string.IsNullOrWhiteSpace(date))
        {
            string[] parts = date.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 1 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
            {
                int? month = parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedMonth) ? parsedMonth : null;
                int? day = parts.Length >= 3 && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedDay) ? parsedDay : null;
                HistoryDatePrecision precision = day.HasValue ? HistoryDatePrecision.Day : month.HasValue ? HistoryDatePrecision.Month : HistoryDatePrecision.Year;
                return IsValidHistoryDate(year, month, day, precision) ? new HistoryDateParts(year, month, day, precision) : null;
            }
        }

        int? explicitYear = ReadInt(patch, "year");
        if (!explicitYear.HasValue)
        {
            return null;
        }

        int? explicitMonth = ReadInt(patch, "month");
        int? explicitDay = ReadInt(patch, "day");
        HistoryDatePrecision explicitPrecision = ReadEnum(patch, "datePrecision", ReadEnum(patch, "precision", explicitDay.HasValue ? HistoryDatePrecision.Day : explicitMonth.HasValue ? HistoryDatePrecision.Month : HistoryDatePrecision.Year));
        return IsValidHistoryDate(explicitYear.Value, explicitMonth, explicitDay, explicitPrecision)
            ? new HistoryDateParts(explicitYear.Value, explicitMonth, explicitDay, explicitPrecision)
            : null;
    }

    private static bool IsValidHistoryDate(int year, int? month, int? day, HistoryDatePrecision precision)
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

    private static List<LocalizedText> ReadLocalizedTextsFlexible(JsonElement element, string arrayPropertyName, string compactPropertyName)
    {
        JsonElement? array = GetArray(element, arrayPropertyName);
        if (array is not null)
        {
            return ReadLocalizedTexts(array);
        }

        if (!element.TryGetProperty(compactPropertyName, out JsonElement compact))
        {
            return new List<LocalizedText>();
        }

        if (compact.ValueKind == JsonValueKind.String)
        {
            string? value = NormalizeString(compact.GetString());
            return string.IsNullOrWhiteSpace(value)
                ? new List<LocalizedText>()
                : new List<LocalizedText> { new LocalizedText("fr", value) };
        }

        if (compact.ValueKind != JsonValueKind.Object)
        {
            return new List<LocalizedText>();
        }

        List<LocalizedText> values = new List<LocalizedText>();
        foreach (JsonProperty property in compact.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? text = NormalizeString(property.Value.GetString());
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(property.Name))
            {
                values.Add(new LocalizedText(property.Name.Trim().ToLowerInvariant(), text));
            }
        }

        return values;
    }

    private static HistoryArticle? ReadHistoryArticle(JsonElement? element)
    {
        if (element is null)
        {
            return null;
        }

        return new HistoryArticle
        {
            Slug = NormalizeString(ReadString(element, "slug")),
            Titles = ReadLocalizedTextsFlexible(element.Value, "titles", "title"),
            Subtitles = ReadLocalizedTextsFlexible(element.Value, "subtitles", "subtitle"),
            Summaries = ReadLocalizedTextsFlexible(element.Value, "summaries", "summary"),
            MainImageId = NormalizeString(ReadString(element, "mainImageId")),
            Blocks = ReadHistoryArticleBlocks(GetArray(element, "blocks")),
            Sources = ReadHistorySources(GetArray(element, "sources")),
            IsPublished = ReadBool(element, "isPublished") ?? true,
        };
    }

    private static List<HistoryArticleBlock> ReadHistoryArticleBlocks(JsonElement? array)
    {
        if (array is null)
        {
            return new List<HistoryArticleBlock>();
        }

        List<HistoryArticleBlock> blocks = new List<HistoryArticleBlock>();
        int fallbackSortOrder = 0;
        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            fallbackSortOrder++;
            HistoryArticleBlock block = new HistoryArticleBlock
            {
                Id = NormalizeString(ReadString(item, "id")) ?? Guid.NewGuid().ToString("N"),
                Type = ReadEnum(item, "type", HistoryArticleBlockType.Paragraph),
                SortOrder = ReadInt(item, "sortOrder") ?? fallbackSortOrder,
                HeadingLevel = ReadInt(item, "headingLevel"),
                Texts = ReadLocalizedTextsFlexible(item, "texts", "text"),
                ImageId = NormalizeString(ReadString(item, "imageId")),
                ImageIds = ReadStringArray(GetArray(item, "imageIds")),
                Captions = ReadLocalizedTextsFlexible(item, "captions", "caption"),
            };
            blocks.Add(block);
        }

        return blocks;
    }

    private static List<HistorySourceReference> ReadHistorySources(JsonElement? array)
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

            string? url = NormalizeString(ReadString(item, "url"));
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            sources.Add(new HistorySourceReference
            {
                Label = NormalizeString(ReadString(item, "label")),
                Url = url,
                AccessedAt = NormalizeString(ReadString(item, "accessedAt")),
            });
        }

        return sources;
    }

    private static string BuildHistoryKey(HistoryEntityType entityType, string ownerId, string eventType, HistoryDateParts dateParts)
    {
        string month = dateParts.Month.HasValue ? dateParts.Month.Value.ToString("00", CultureInfo.InvariantCulture) : "00";
        string day = dateParts.Day.HasValue ? dateParts.Day.Value.ToString("00", CultureInfo.InvariantCulture) : "00";
        return $"{entityType}-{ownerId}-{dateParts.Year.ToString(CultureInfo.InvariantCulture)}-{month}-{day}-{NormalizeKey(eventType)}";
    }

    private static string ResolveHistoryDisplayName(JsonElement patch, string fallback)
    {
        List<LocalizedText> titles = ReadLocalizedTextsFlexible(patch, "titles", "title");
        return titles.FirstOrDefault(static title => !string.IsNullOrWhiteSpace(title.Value))?.Value ?? fallback;
    }

    private sealed record HistoryDateParts(int Year, int? Month, int? Day, HistoryDatePrecision Precision);
}
