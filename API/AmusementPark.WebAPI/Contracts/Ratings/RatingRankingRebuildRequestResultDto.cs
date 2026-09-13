namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingRebuildRequestResultDto
{
    public DateTime RequestedAtUtc { get; set; }

    public int ScheduledScopeCount { get; set; }

    public IReadOnlyCollection<RatingRankingScheduledScopeDto> Scopes { get; set; } =
        Array.Empty<RatingRankingScheduledScopeDto>();
}
