namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Couverture possible d'une sélection civile par une période historique.
/// </summary>
public enum HistoricalPeriodMatch
{
    Outside = 1,
    PossibleOverlap = 2,
    EntirelyContained = 3,
}
