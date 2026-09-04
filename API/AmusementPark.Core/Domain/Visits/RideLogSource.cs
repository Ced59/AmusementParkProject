namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Origine métier d'une occurrence. Les valeurs futures restent additives.
/// </summary>
public enum RideLogSource
{
    Manual = 1,
    Import = 2,
}
