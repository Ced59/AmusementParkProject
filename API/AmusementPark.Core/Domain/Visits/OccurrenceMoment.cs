namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Heure locale facultative déclarée pour une occurrence, sans instant UTC inventé.
/// </summary>
public sealed record OccurrenceMoment(
    TimeOnly? LocalTime,
    bool IsApproximate);
