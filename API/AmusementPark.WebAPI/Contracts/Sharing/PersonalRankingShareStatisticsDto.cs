namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PersonalRankingShareStatisticsDto
{
    public long TotalRatings { get; set; }

    public double AverageRating { get; set; }

    public double HighestRating { get; set; }

    public double LowestRating { get; set; }

    public List<PersonalRankingShareStatBucketDto> ByPark { get; set; } = new();

    public List<PersonalRankingShareStatBucketDto> ByTargetType { get; set; } = new();

    public List<PersonalRankingShareStatBucketDto> ByParkItemCategory { get; set; } = new();
}
