namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripExportDayResult(
    DateOnly LocalDate,
    string? ParkName,
    bool IsParkAvailable,
    TimeOnly? DesiredArrivalTime,
    string? GroupNote,
    IReadOnlyCollection<TripExportDayBlockResult> Blocks);
