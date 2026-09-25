namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Relation d'ordre pouvant être établie entre les bornes d'une période.
/// </summary>
public enum HistoricalPeriodOrdering
{
    Point = 1,
    Ordered = 2,
    Ambiguous = 3,
}
