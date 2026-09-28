using AmusementPark.Core.Domain.History;

namespace AmusementPark.Core.Domain.Visits;

public static class PassportHistoricalEvidencePolicy
{
    public static bool HasCanonicalVisitEvidence(HistoricalSubjectSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.OperationalState is HistoricalOperationalState.KnownOpen
                or HistoricalOperationalState.KnownClosed
            && snapshot.SupportingFactIds.Count > 0;
    }

    public static bool HasCanonicalAttributeEvidence(
        HistoricalSubjectSnapshot snapshot,
        HistoricalAttributeKind kind)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        HistoricalAttributeSnapshot? attribute = snapshot.Attributes
            .SingleOrDefault(value => value.Kind == kind);
        return attribute?.State == HistoricalAttributeValueState.Known
            && attribute.SupportingFactIds.Count > 0;
    }
}
