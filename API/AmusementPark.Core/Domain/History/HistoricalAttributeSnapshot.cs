namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalAttributeSnapshot
{
    public HistoricalAttributeSnapshot(
        HistoricalAttributeKind kind,
        HistoricalAttributeValueState state,
        string? value,
        IReadOnlyCollection<string> candidates,
        IReadOnlyCollection<HistoricalSnapshotReason> reasons,
        IReadOnlyCollection<Guid> supportingFactIds)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(reasons);
        ArgumentNullException.ThrowIfNull(supportingFactIds);
        string? normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        string[] normalizedCandidates = candidates
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(static candidate => candidate.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static candidate => candidate, StringComparer.Ordinal)
            .ToArray();
        bool stateIsInconsistent = state == HistoricalAttributeValueState.Known
            && (normalizedValue is null
                || normalizedCandidates.Length != 1
                || !string.Equals(normalizedValue, normalizedCandidates[0], StringComparison.Ordinal))
            || state != HistoricalAttributeValueState.Known && normalizedValue is not null
            || state == HistoricalAttributeValueState.Unknown && normalizedCandidates.Length != 0;
        if (stateIsInconsistent)
        {
            throw new ArgumentException("The historical attribute snapshot state is inconsistent.", nameof(state));
        }

        this.Kind = kind;
        this.State = state;
        this.Value = normalizedValue;
        this.Candidates = Array.AsReadOnly(normalizedCandidates);
        this.Reasons = Array.AsReadOnly(
            reasons.OrderBy(static reason => reason.Code).ToArray());
        this.SupportingFactIds = Array.AsReadOnly(
            supportingFactIds
                .Where(static factId => factId != Guid.Empty)
                .Distinct()
                .OrderBy(static factId => factId)
                .ToArray());
    }

    public HistoricalAttributeKind Kind { get; }

    public HistoricalAttributeValueState State { get; }

    public string? Value { get; }

    public IReadOnlyList<string> Candidates { get; }

    public IReadOnlyList<HistoricalSnapshotReason> Reasons { get; }

    public IReadOnlyList<Guid> SupportingFactIds { get; }
}
