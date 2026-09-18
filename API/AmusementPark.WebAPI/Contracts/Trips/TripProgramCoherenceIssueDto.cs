namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripProgramCoherenceIssueDto
{
    public string Code { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public DateOnly? LocalDate { get; init; }

    public string? ParkId { get; init; }

    public string? ParkName { get; init; }

    public string? ParkItemId { get; init; }

    public string? ParkItemName { get; init; }

    public string? OfficialStatus { get; init; }

    public string? OfficialSourceUrl { get; init; }

    public DateTime? OfficialVerifiedAtUtc { get; init; }
}
