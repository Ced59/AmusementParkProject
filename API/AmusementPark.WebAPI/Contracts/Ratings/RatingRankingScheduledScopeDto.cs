namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingScheduledScopeDto
{
    public string ScopeKey { get; set; } = string.Empty;

    public long RequestedSourceRevision { get; set; }
}
