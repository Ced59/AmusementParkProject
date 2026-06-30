using System.Globalization;
using System.Text.RegularExpressions;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.History.Handlers;

internal static class AutomaticHistoryEventFactory
{
    private static readonly string[] SupportedLanguages = new[]
    {
        "fr",
        "en",
        "de",
        "nl",
        "it",
        "es",
        "pl",
        "pt",
    };

    public static bool HasLifecycleDate(ParkItem parkItem)
    {
        AttractionDetails? details = parkItem.AttractionDetails;
        return details?.OpeningDate is not null || details?.ClosingDate is not null;
    }

    public static IReadOnlyCollection<HistoryEvent> CreateParkItemLifecycleEvents(ParkItem parkItem)
    {
        if (string.IsNullOrWhiteSpace(parkItem.Id) || !HasLifecycleDate(parkItem))
        {
            return Array.Empty<HistoryEvent>();
        }

        List<HistoryEvent> events = new List<HistoryEvent>();
        AttractionDetails details = parkItem.AttractionDetails!;

        if (details.OpeningDate.HasValue)
        {
            HistoryDateParts dateParts = ResolveDateParts(details.OpeningDate.Value, details.OpeningDateText);
            events.Add(CreateLifecycleEvent(
                parkItem,
                dateParts,
                ParkItemHistoryEventType.Opening.ToString(),
                "opening"));
        }

        if (details.ClosingDate.HasValue)
        {
            HistoryDateParts dateParts = ResolveDateParts(details.ClosingDate.Value, details.ClosingDateText);
            events.Add(CreateLifecycleEvent(
                parkItem,
                dateParts,
                ParkItemHistoryEventType.DefinitiveClosure.ToString(),
                "closure"));
        }

        return events;
    }

    public static IReadOnlyCollection<HistoryEvent> CreateParkItemLifecycleEvents(IEnumerable<ParkItem> parkItems)
    {
        List<HistoryEvent> events = new List<HistoryEvent>();
        foreach (ParkItem parkItem in parkItems)
        {
            events.AddRange(CreateParkItemLifecycleEvents(parkItem));
        }

        return events;
    }

    public static List<HistoryEvent> MergeWithExplicitEvents(
        IReadOnlyCollection<HistoryEvent> explicitEvents,
        IReadOnlyCollection<HistoryEvent> automaticEvents)
    {
        List<HistoryEvent> mergedEvents = explicitEvents.ToList();

        foreach (HistoryEvent automaticEvent in automaticEvents)
        {
            bool alreadyCovered = mergedEvents.Any(existingEvent => CoversAutomaticEvent(existingEvent, automaticEvent));
            if (!alreadyCovered)
            {
                mergedEvents.Add(automaticEvent);
            }
        }

        return mergedEvents;
    }

    private static HistoryEvent CreateLifecycleEvent(
        ParkItem parkItem,
        HistoryDateParts dateParts,
        string eventType,
        string keySuffix)
    {
        string normalizedParkId = parkItem.ParkId?.Trim() ?? string.Empty;
        string key = BuildKey(parkItem.Id, normalizedParkId, keySuffix, dateParts);
        DateTime timestamp = parkItem.UpdatedAtUtc == default ? DateTime.UtcNow : parkItem.UpdatedAtUtc;
        string itemName = string.IsNullOrWhiteSpace(parkItem.Name) ? parkItem.Id : parkItem.Name.Trim();

        return new HistoryEvent
        {
            Id = key,
            Key = key,
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = parkItem.Id,
            ParkId = normalizedParkId,
            ParkItemId = parkItem.Id,
            ContextParkId = normalizedParkId,
            Year = dateParts.Year,
            Month = dateParts.Precision == HistoryDatePrecision.Year ? null : dateParts.Month,
            Day = dateParts.Precision == HistoryDatePrecision.Day ? dateParts.Day : null,
            DatePrecision = dateParts.Precision,
            EventType = eventType,
            IsMajor = false,
            IsVisible = parkItem.IsVisible,
            Slug = key,
            Titles = BuildTitles(itemName, eventType),
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
        };
    }

    private static string BuildKey(string parkItemId, string parkId, string suffix, HistoryDateParts dateParts)
    {
        string dateKey = dateParts.Precision switch
        {
            HistoryDatePrecision.Day => string.Create(
                CultureInfo.InvariantCulture,
                $"{dateParts.Year:0000}-{dateParts.Month.GetValueOrDefault():00}-{dateParts.Day.GetValueOrDefault():00}"),
            HistoryDatePrecision.Month => string.Create(
                CultureInfo.InvariantCulture,
                $"{dateParts.Year:0000}-{dateParts.Month.GetValueOrDefault():00}"),
            _ => dateParts.Year.ToString("0000", CultureInfo.InvariantCulture),
        };

        string contextPart = string.IsNullOrWhiteSpace(parkId) ? "unknown-park" : parkId.Trim();
        return $"auto-parkitem-{parkItemId.Trim()}-{contextPart}-{suffix}-{dateKey}";
    }

    private static List<LocalizedText> BuildTitles(string itemName, string eventType)
    {
        Dictionary<string, string> templates = eventType == ParkItemHistoryEventType.Opening.ToString()
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["fr"] = "Ouverture de {0}",
                ["en"] = "{0} opens",
                ["de"] = "Eröffnung von {0}",
                ["nl"] = "Opening van {0}",
                ["it"] = "Apertura di {0}",
                ["es"] = "Apertura de {0}",
                ["pl"] = "Otwarcie {0}",
                ["pt"] = "Abertura de {0}",
            }
            : new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["fr"] = "Fermeture de {0}",
                ["en"] = "{0} closes",
                ["de"] = "Schließung von {0}",
                ["nl"] = "Sluiting van {0}",
                ["it"] = "Chiusura di {0}",
                ["es"] = "Cierre de {0}",
                ["pl"] = "Zamknięcie {0}",
                ["pt"] = "Encerramento de {0}",
            };

        List<LocalizedText> titles = new List<LocalizedText>();
        foreach (string language in SupportedLanguages)
        {
            string template = templates.TryGetValue(language, out string? localizedTemplate)
                ? localizedTemplate
                : templates["en"];
            titles.Add(new LocalizedText(language, string.Format(CultureInfo.InvariantCulture, template, itemName)));
        }

        return titles;
    }

    private static HistoryDateParts ResolveDateParts(DateTime date, string? dateText)
    {
        DateTime normalizedDate = date.Date;
        string normalizedText = dateText?.Trim() ?? string.Empty;

        if (Regex.IsMatch(normalizedText, @"^\d{4}$"))
        {
            return new HistoryDateParts(normalizedDate.Year, null, null, HistoryDatePrecision.Year);
        }

        if (Regex.IsMatch(normalizedText, @"^\d{4}[-/]\d{1,2}$") ||
            Regex.IsMatch(normalizedText, @"^\d{1,2}[-/]\d{4}$"))
        {
            return new HistoryDateParts(normalizedDate.Year, normalizedDate.Month, null, HistoryDatePrecision.Month);
        }

        return new HistoryDateParts(normalizedDate.Year, normalizedDate.Month, normalizedDate.Day, HistoryDatePrecision.Day);
    }

    private static bool CoversAutomaticEvent(HistoryEvent existingEvent, HistoryEvent automaticEvent)
    {
        if (string.Equals(existingEvent.Key, automaticEvent.Key, StringComparison.Ordinal))
        {
            return true;
        }

        if (existingEvent.EntityType != automaticEvent.EntityType ||
            !string.Equals(existingEvent.OwnerId, automaticEvent.OwnerId, StringComparison.Ordinal) ||
            !string.Equals(ResolveContextParkId(existingEvent), ResolveContextParkId(automaticEvent), StringComparison.Ordinal))
        {
            return false;
        }

        if (!IsSameLifecycleKind(existingEvent.EventType, automaticEvent.EventType))
        {
            return false;
        }

        if (existingEvent.Year != automaticEvent.Year)
        {
            return false;
        }

        if (existingEvent.Month.HasValue &&
            automaticEvent.Month.HasValue &&
            existingEvent.Month.Value != automaticEvent.Month.Value)
        {
            return false;
        }

        if (existingEvent.Day.HasValue &&
            automaticEvent.Day.HasValue &&
            existingEvent.Day.Value != automaticEvent.Day.Value)
        {
            return false;
        }

        return true;
    }

    private static bool IsSameLifecycleKind(string explicitEventType, string automaticEventType)
    {
        if (string.Equals(automaticEventType, ParkItemHistoryEventType.Opening.ToString(), StringComparison.Ordinal))
        {
            return string.Equals(explicitEventType, ParkItemHistoryEventType.Opening.ToString(), StringComparison.Ordinal);
        }

        return string.Equals(explicitEventType, ParkItemHistoryEventType.Closure.ToString(), StringComparison.Ordinal) ||
               string.Equals(explicitEventType, ParkItemHistoryEventType.DefinitiveClosure.ToString(), StringComparison.Ordinal);
    }

    private static string? ResolveContextParkId(HistoryEvent historyEvent)
    {
        return string.IsNullOrWhiteSpace(historyEvent.ContextParkId)
            ? historyEvent.ParkId
            : historyEvent.ContextParkId;
    }

    private sealed record HistoryDateParts(int Year, int? Month, int? Day, HistoryDatePrecision Precision);
}
