using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripProgramCoherenceIssueResult(
    TripProgramCoherenceCode Code,
    TripProgramCoherenceSeverity Severity,
    DateOnly? LocalDate,
    string? ParkId,
    string? ParkName,
    string? ParkItemId,
    string? ParkItemName,
    string? OfficialStatus,
    string? OfficialSourceUrl,
    DateTime? OfficialVerifiedAtUtc);
