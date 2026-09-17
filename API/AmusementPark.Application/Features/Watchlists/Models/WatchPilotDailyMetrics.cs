namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record WatchPilotDailyMetrics(
    string Date,
    long NotificationsDelivered,
    long DigestsGenerated,
    IReadOnlyDictionary<string, long> InteractionCounts);
