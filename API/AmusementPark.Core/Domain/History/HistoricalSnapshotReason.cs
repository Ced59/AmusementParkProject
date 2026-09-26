namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalSnapshotReason
{
    public HistoricalSnapshotReason(
        HistoricalSnapshotReasonCode code,
        IReadOnlyCollection<Guid> factIds)
    {
        if (!Enum.IsDefined(code))
        {
            throw new ArgumentOutOfRangeException(nameof(code));
        }

        ArgumentNullException.ThrowIfNull(factIds);
        this.Code = code;
        this.FactIds = Array.AsReadOnly(
            factIds
                .Where(static factId => factId != Guid.Empty)
                .Distinct()
                .OrderBy(static factId => factId)
                .ToArray());
    }

    public HistoricalSnapshotReasonCode Code { get; }

    public IReadOnlyList<Guid> FactIds { get; }
}
