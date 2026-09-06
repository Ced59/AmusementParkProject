namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PersonalRankingSharePreviewDto
{
    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public PersonalRankingShareStatisticsDto? Statistics { get; set; }

    public List<PersonalRankingSharePreviewItemDto> Ratings { get; set; } = new();

    public bool IsTruncated { get; set; }
}
