namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PersonalRankingShareStatBucketDto
{
    public string? Key { get; set; }

    public string Label { get; set; } = string.Empty;

    public long Count { get; set; }

    public double AverageRating { get; set; }
}
