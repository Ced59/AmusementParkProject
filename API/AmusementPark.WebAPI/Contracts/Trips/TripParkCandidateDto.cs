namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripParkCandidateDto
{
    public string CandidateId { get; set; } = string.Empty;
    public string ParkId { get; set; } = string.Empty;
    public string ParkName { get; set; } = string.Empty;
    public IReadOnlyCollection<string> CandidateDates { get; set; } = Array.Empty<string>();
    public string Source { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? CollectiveNote { get; set; }
    public TripFitRecommendationSnapshotDto? FitSnapshot { get; set; }
    public long SortPosition { get; set; }
    public long Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
