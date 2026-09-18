namespace AmusementPark.Core.Domain.Trips;

public sealed record TripProgramCoherenceIssue(
    TripProgramCoherenceCode Code,
    TripProgramCoherenceSeverity Severity,
    DateOnly? LocalDate,
    string? ParkId,
    string? ParkItemId);
