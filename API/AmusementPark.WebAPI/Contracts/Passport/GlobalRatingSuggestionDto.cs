using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class GlobalRatingSuggestionDto
{
    public GlobalRatingSuggestionTargetTypeDto TargetType { get; init; }
    public string TargetId { get; init; } = string.Empty;
    public string TargetName { get; init; } = string.Empty;
    public string ParkId { get; init; } = string.Empty;
    public string? ParkName { get; init; }
    public string? ParkItemCategory { get; init; }
    public double CurrentGlobalRating { get; init; }
    public double LatestObservationRating { get; init; }
    public double RecentAverage { get; init; }
    public double HistoricalMedian { get; init; }
    public int NewObservationCount { get; init; }
    public int RecentObservationCount { get; init; }
    public GlobalRatingSuggestionReasonDto Reason { get; init; }
    public DateTime LatestObservationAtUtc { get; init; }
}
