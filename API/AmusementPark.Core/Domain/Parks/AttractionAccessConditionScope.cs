namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Portée matérielle ou temporelle d'une condition d'accès.
/// </summary>
public enum AttractionAccessConditionScope
{
    Attraction,
    Vehicle,
    Seat,
    OperatingPeriod,
    Custom,
}
