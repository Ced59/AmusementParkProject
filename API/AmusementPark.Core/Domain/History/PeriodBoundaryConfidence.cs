namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Niveau de confiance porté par une borne de période historique.
/// </summary>
public enum PeriodBoundaryConfidence
{
    Confirmed = 1,
    Estimated = 2,
    Disputed = 3,
}
