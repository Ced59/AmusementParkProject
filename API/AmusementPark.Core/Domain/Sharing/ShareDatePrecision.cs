namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Précision calendaire maximale qu'une politique autorise à publier.
/// </summary>
public enum ShareDatePrecision
{
    Hidden = 0,
    Year = 1,
    Month = 2,
    Day = 3,
}
