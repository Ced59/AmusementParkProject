namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripExportDto
{
    public string SchemaVersion { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public TripDateProposalDto DateProposal { get; set; } = new();

    public string? DestinationTimeZoneId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime GeneratedAtUtc { get; set; }

    public IReadOnlyCollection<TripExportCandidateDto> CandidateParks { get; set; } =
        Array.Empty<TripExportCandidateDto>();

    public IReadOnlyCollection<TripExportDayDto> Days { get; set; } =
        Array.Empty<TripExportDayDto>();

    public IReadOnlyCollection<TripExportDecisionDto> CollectiveDecisions { get; set; } =
        Array.Empty<TripExportDecisionDto>();
}
