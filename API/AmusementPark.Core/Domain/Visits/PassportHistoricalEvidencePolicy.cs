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
}
