namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class SharedProfileComparisonDto
{
    public DateTime CreatedAtUtc { get; set; }

    public string? CreatorDisplayName { get; set; }

    public string? AcceptorDisplayName { get; set; }

    public List<string> Categories { get; set; } = new();

    public List<ProfileComparisonParkDto> Parks { get; set; } = new();

    public List<ProfileComparisonRatingDto> Ratings { get; set; } = new();

    public List<ProfileComparisonYearDto> Years { get; set; } = new();

    public List<ProfileComparisonMissedItemDto> MissedItems { get; set; } = new();

    public int CommonRatingCount { get; set; }

    public int MinimumRatingsForCorrelation { get; set; }

    public double? RatingCorrelation { get; set; }

    public bool HasIncompleteCatalog { get; set; }

    public string CalculationVersion { get; set; } = string.Empty;
}
