namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripExportCandidateDto
{
    public string? ParkName { get; set; }

    public bool IsParkAvailable { get; set; }

    public IReadOnlyCollection<string> CandidateDates { get; set; } = Array.Empty<string>();

    public string State { get; set; } = string.Empty;

    public string? CollectiveNote { get; set; }
}
