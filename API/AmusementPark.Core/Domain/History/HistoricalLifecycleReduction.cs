namespace AmusementPark.Core.Domain.History;

internal sealed record HistoricalLifecycleReduction(
    HistoricalOperationalState State,
    HistoricalPresenceExtent PresenceExtent,
    IReadOnlyList<HistoricalPresenceInterval> ConfirmedPresenceIntervals,
    IReadOnlyList<HistoricalSnapshotReason> Reasons,
    IReadOnlyList<Guid> SupportingFactIds);
