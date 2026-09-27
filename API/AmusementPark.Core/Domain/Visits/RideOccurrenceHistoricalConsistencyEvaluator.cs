using AmusementPark.Core.Domain.History;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Traduit l'état canonique d'un sujet à la date de visite en cohérence Passeport.
/// </summary>
public static class RideOccurrenceHistoricalConsistencyEvaluator
{
    public static HistoricalConsistency Evaluate(HistoricalOperationalState operationalState)
    {
        if (!Enum.IsDefined(operationalState))
        {
            throw new ArgumentOutOfRangeException(nameof(operationalState));
        }

        return operationalState switch
        {
            HistoricalOperationalState.KnownOpen => HistoricalConsistency.Verified,
            HistoricalOperationalState.KnownClosed => HistoricalConsistency.ConfirmedConflict,
            HistoricalOperationalState.PossiblyOpen => HistoricalConsistency.Unverified,
            HistoricalOperationalState.Unknown => HistoricalConsistency.Unverified,
            _ => throw new ArgumentOutOfRangeException(nameof(operationalState)),
        };
    }
}
