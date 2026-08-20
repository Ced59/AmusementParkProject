using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

internal static class StandaloneAttractionAutomaticHistoryEventFactory
{
    public static bool HasLifecycleDate(StandaloneAttraction attraction)
    {
        ArgumentNullException.ThrowIfNull(attraction);
        return AutomaticHistoryEventFactory.HasLifecycleDate(ToShadowParkItem(attraction));
    }

    public static IReadOnlyCollection<HistoryEvent> CreateLifecycleEvents(StandaloneAttraction attraction)
    {
        ArgumentNullException.ThrowIfNull(attraction);
        if (string.IsNullOrWhiteSpace(attraction.Id) || !HasLifecycleDate(attraction))
        {
            return Array.Empty<HistoryEvent>();
        }

        IReadOnlyCollection<HistoryEvent> sourceEvents = AutomaticHistoryEventFactory.CreateParkItemLifecycleEvents(ToShadowParkItem(attraction));
        List<HistoryEvent> events = new List<HistoryEvent>();
        foreach (HistoryEvent sourceEvent in sourceEvents)
        {
            string suffix = string.Equals(sourceEvent.EventType, ParkItemHistoryEventType.Opening.ToString(), StringComparison.Ordinal)
                ? "opening"
                : "closure";
            string key = BuildKey(attraction.Id, suffix, sourceEvent);
            sourceEvent.Id = key;
            sourceEvent.Key = key;
            sourceEvent.EntityType = HistoryEntityType.StandaloneAttraction;
            sourceEvent.OwnerId = attraction.Id;
            sourceEvent.ParkId = null;
            sourceEvent.ParkItemId = null;
            sourceEvent.ContextParkId = null;
            events.Add(sourceEvent);
        }

        return events;
    }

    private static ParkItem ToShadowParkItem(StandaloneAttraction attraction)
    {
        return new ParkItem
        {
            Id = attraction.Id,
            Name = attraction.Name,
            Category = ParkItemCategory.Attraction,
            Type = attraction.Type,
            IsVisible = attraction.IsVisible,
            AttractionDetails = attraction.AttractionDetails,
        };
    }

    private static string BuildKey(string attractionId, string suffix, HistoryEvent historyEvent)
    {
        string dateKey = historyEvent.DatePrecision switch
        {
            HistoryDatePrecision.Day => string.Create(
                CultureInfo.InvariantCulture,
                $"{historyEvent.Year:0000}-{historyEvent.Month.GetValueOrDefault():00}-{historyEvent.Day.GetValueOrDefault():00}"),
            HistoryDatePrecision.Month => string.Create(
                CultureInfo.InvariantCulture,
                $"{historyEvent.Year:0000}-{historyEvent.Month.GetValueOrDefault():00}"),
            _ => historyEvent.Year.ToString("0000", CultureInfo.InvariantCulture),
        };

        return $"auto-standalone-{attractionId.Trim()}-{suffix}-{dateKey}";
    }
}
