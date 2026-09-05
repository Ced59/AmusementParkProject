namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class SharedUserRankingProfileDto
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime PublishedAtUtc { get; set; }

    public bool IsOwner { get; set; }

    public UserRatingStatsDto Stats { get; set; } = new UserRatingStatsDto();
}
