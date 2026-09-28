using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal static class ParkGraphHistoryExportLoader
{
    public static async Task<IReadOnlyCollection<HistoryEvent>> LoadAsync(
        IHistoryEventRepository historyEventRepository,
        string parkId,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        Task<IReadOnlyCollection<HistoryEvent>> parkTimelineTask = historyEventRepository.GetParkTimelineAsync(
            parkId,
            true,
            true,
            parkItemIds,
            cancellationToken);
        Task<IReadOnlyCollection<HistoryEvent>> ownedParkItemTimelinesTask = parkItemIds.Count == 0
            ? Task.FromResult<IReadOnlyCollection<HistoryEvent>>(Array.Empty<HistoryEvent>())
            : historyEventRepository.GetOwnerTimelinesAsync(
                HistoryEntityType.ParkItem,
                parkItemIds,
                true,
                cancellationToken);

        await Task.WhenAll(parkTimelineTask, ownedParkItemTimelinesTask);

        IReadOnlyCollection<HistoryEvent> parkTimeline = await parkTimelineTask;
        IReadOnlyCollection<HistoryEvent> ownedParkItemTimelines = await ownedParkItemTimelinesTask;

        return parkTimeline
            .Concat(ownedParkItemTimelines)
            .GroupBy(BuildEventIdentity, StringComparer.Ordinal)
            .Select(static group => group.First())
            .OrderBy(static historyEvent => historyEvent.Year)
            .ThenBy(static historyEvent => historyEvent.Month)
            .ThenBy(static historyEvent => historyEvent.Day)
            .ThenBy(static historyEvent => historyEvent.Key, StringComparer.Ordinal)
            .ThenBy(static historyEvent => historyEvent.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static string BuildEventIdentity(HistoryEvent historyEvent)
    {
        return !string.IsNullOrWhiteSpace(historyEvent.Id)
            ? $"id:{historyEvent.Id.Trim()}"
            : $"owner:{historyEvent.EntityType}:{historyEvent.OwnerId.Trim()}:{historyEvent.Key.Trim()}";
    }
}
