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
        return details?.OpeningDate is not null ||
               details?.ClosingDate is not null ||
               TryResolveTextDateParts(details?.OpeningDateText, out _) ||
               TryResolveTextDateParts(details?.ClosingDateText, out _);
    }

    public static bool HasLifecycleDate(Park park)
    {
        return park.OpeningDate is not null ||
               park.ClosingDate is not null ||
               TryResolveTextDateParts(park.OpeningDateText, out _) ||
               TryResolveTextDateParts(park.ClosingDateText, out _);
    }

    public static IReadOnlyCollection<HistoryEvent> CreateParkLifecycleEvents(Park park)
    {
        if (string.IsNullOrWhiteSpace(park.Id) || !HasLifecycleDate(park))
        {
            return Array.Empty<HistoryEvent>();
        }

        List<HistoryEvent> events = new List<HistoryEvent>();

        if (TryResolveDateParts(park.OpeningDate, park.OpeningDateText, out HistoryDateParts openingDateParts))
        {
            events.Add(CreateLifecycleEvent(
                park,
                openingDateParts,
                ParkHistoryEventType.Opening.ToString(),
                "opening"));
        }

        if (TryResolveDateParts(park.ClosingDate, park.ClosingDateText, out HistoryDateParts closingDateParts))
        {
            events.Add(CreateLifecycleEvent(
                park,
                closingDateParts,
                ParkHistoryEventType.DefinitiveClosure.ToString(),
                "closure"));
        }

        return events;
    }

    public static IReadOnlyCollection<HistoryEvent> CreateParkLifecycleEvents(IEnumerable<Park> parks)
    {
        List<HistoryEvent> events = new List<HistoryEvent>();
        foreach (Park park in parks)
        {
            events.AddRange(CreateParkLifecycleEvents(park));
        }

        return events;
    }

    public static IReadOnlyCollection<HistoryEvent> CreateParkItemLifecycleEvents(ParkItem parkItem)
    {
        if (string.IsNullOrWhiteSpace(parkItem.Id) || !HasLifecycleDate(parkItem))
        {
            return Array.Empty<HistoryEvent>();
        }

        List<HistoryEvent> events = new List<HistoryEvent>();
        AttractionDetails details = parkItem.AttractionDetails!;

        if (TryResolveDateParts(details.OpeningDate, details.OpeningDateText, out HistoryDateParts openingDateParts))
        {
            events.Add(CreateLifecycleEvent(
                parkItem,
                openingDateParts,
                ParkItemHistoryEventType.Opening.ToString(),
                "opening"));
        }

        if (TryResolveDateParts(details.ClosingDate, details.ClosingDateText, out HistoryDateParts closingDateParts))
        {
            events.Add(CreateLifecycleEvent(
                parkItem,
                closingDateParts,
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
        Park park,
        HistoryDateParts dateParts,
        string eventType,
        string keySuffix)
    {
        string key = BuildParkKey(park.Id, keySuffix, dateParts);
        DateTime timestamp = park.UpdatedAtUtc == default ? DateTime.UtcNow : park.UpdatedAtUtc;
        string parkName = string.IsNullOrWhiteSpace(park.Name) ? park.Id : park.Name.Trim();

        return new HistoryEvent
        {
            Id = key,
            Key = key,
            EntityType = HistoryEntityType.Park,
            OwnerId = park.Id,
            ParkId = park.Id,
            ParkItemId = null,
            ContextParkId = null,
            Year = dateParts.Year,
            Month = dateParts.Precision == HistoryDatePrecision.Year ? null : dateParts.Month,
            Day = dateParts.Precision == HistoryDatePrecision.Day ? dateParts.Day : null,
            DatePrecision = dateParts.Precision,
            EventType = eventType,
            IsMajor = false,
            IsVisible = park.IsVisible,
            Slug = key,
            Titles = BuildParkTitles(parkName, eventType),
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
        };
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

    private static string BuildParkKey(string parkId, string suffix, HistoryDateParts dateParts)
    {
        string dateKey = BuildDateKey(dateParts);
        return $"auto-park-{parkId.Trim()}-{suffix}-{dateKey}";
    }

    private static string BuildKey(string parkItemId, string parkId, string suffix, HistoryDateParts dateParts)
    {
        string dateKey = BuildDateKey(dateParts);
        string contextPart = string.IsNullOrWhiteSpace(parkId) ? "unknown-park" : parkId.Trim();
        return $"auto-parkitem-{parkItemId.Trim()}-{contextPart}-{suffix}-{dateKey}";
    }

    private static string BuildDateKey(HistoryDateParts dateParts)
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

        return dateKey;
    }

    private static List<LocalizedText> BuildParkTitles(string parkName, string eventType)
    {
        Dictionary<string, string> templates = eventType == ParkHistoryEventType.Opening.ToString()
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
                ["fr"] = "Fermeture définitive de {0}",
                ["en"] = "{0} closes definitively",
                ["de"] = "Endgültige Schließung von {0}",
                ["nl"] = "Definitieve sluiting van {0}",
                ["it"] = "Chiusura definitiva di {0}",
                ["es"] = "Cierre definitivo de {0}",
                ["pl"] = "Ostateczne zamknięcie {0}",
                ["pt"] = "Encerramento definitivo de {0}",
            };

        return BuildLocalizedTitles(parkName, templates);
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

        return BuildLocalizedTitles(itemName, templates);
    }

    private static List<LocalizedText> BuildLocalizedTitles(string ownerName, IReadOnlyDictionary<string, string> templates)
    {
        List<LocalizedText> titles = new List<LocalizedText>();
        foreach (string language in SupportedLanguages)
        {
            string template = templates.TryGetValue(language, out string? localizedTemplate)
                ? localizedTemplate
                : templates["en"];
            titles.Add(new LocalizedText(language, string.Format(CultureInfo.InvariantCulture, template, ownerName)));
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

    private static bool TryResolveDateParts(DateTime? date, string? dateText, out HistoryDateParts dateParts)
    {
        if (date.HasValue)
        {
            dateParts = ResolveDateParts(date.Value, dateText);
            return true;
        }

        return TryResolveTextDateParts(dateText, out dateParts);
    }

    private static bool TryResolveTextDateParts(string? dateText, out HistoryDateParts dateParts)
    {
        string normalizedText = dateText?.Trim() ?? string.Empty;
        dateParts = new HistoryDateParts(0, null, null, HistoryDatePrecision.Year);

        Match yearMatch = Regex.Match(normalizedText, @"^(?<year>\d{4})$");
        if (yearMatch.Success &&
            int.TryParse(yearMatch.Groups["year"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
        {
            dateParts = new HistoryDateParts(year, null, null, HistoryDatePrecision.Year);
            return true;
        }

        Match monthMatch = Regex.Match(normalizedText, @"^(?<year>\d{4})[-/](?<month>\d{1,2})$");
        if (monthMatch.Success &&
            int.TryParse(monthMatch.Groups["year"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out year) &&
            int.TryParse(monthMatch.Groups["month"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int month) &&
            month is >= 1 and <= 12)
        {
            dateParts = new HistoryDateParts(year, month, null, HistoryDatePrecision.Month);
            return true;
        }

        Match dayMatch = Regex.Match(normalizedText, @"^(?<year>\d{4})[-/](?<month>\d{1,2})[-/](?<day>\d{1,2})$");
        if (dayMatch.Success &&
            int.TryParse(dayMatch.Groups["year"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out year) &&
            int.TryParse(dayMatch.Groups["month"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out month) &&
            int.TryParse(dayMatch.Groups["day"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day) &&
            DateTime.TryParseExact(
                string.Create(CultureInfo.InvariantCulture, $"{year:0000}-{month:00}-{day:00}"),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            dateParts = new HistoryDateParts(year, month, day, HistoryDatePrecision.Day);
            return true;
        }

        return false;
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
