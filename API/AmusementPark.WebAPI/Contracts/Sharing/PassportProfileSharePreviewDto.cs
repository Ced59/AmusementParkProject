namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileSharePreviewDto
{
    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? PublicCaption { get; set; }

    public string Visibility { get; set; } = string.Empty;

    public bool AllowsComparisons { get; set; }

    public long? ParkCount { get; set; }

    public long? VisitCount { get; set; }

    public long? TotalRideCount { get; set; }

    public long? DistinctItemCount { get; set; }

    public PassportProfileShareRatingSummaryDto? VisitRatings { get; set; }

    public PassportProfileShareRatingSummaryDto? RideRatings { get; set; }

    public List<PassportProfileShareCountryDto> Countries { get; set; } = new();

    public List<PassportProfileShareYearDto> Years { get; set; } = new();

    public List<PassportProfileShareParkDto> Parks { get; set; } = new();

    public List<PassportProfileShareRatingDto> PersonalRanking { get; set; } = new();

    public List<PassportProfileShareMissedItemDto> MissedItems { get; set; } = new();

    public bool HasIncompleteCatalog { get; set; }

    public string CalculationVersion { get; set; } = string.Empty;

    public bool IsEmpty { get; set; }
}
