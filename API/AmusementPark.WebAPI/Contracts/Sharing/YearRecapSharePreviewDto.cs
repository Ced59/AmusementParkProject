namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class YearRecapSharePreviewDto
{
    public int Year { get; set; }

    public long? ParkCount { get; set; }

    public long VisitCount { get; set; }

    public long ApproximateVisitCount { get; set; }

    public double ApproximateVisitRate { get; set; }

    public long? TotalRideCount { get; set; }

    public long? DistinctItemCount { get; set; }

    public long? MissedItemCount { get; set; }

    public List<string> Categories { get; set; } = new();

    public YearRecapShareRatingSummaryDto? ParkRatings { get; set; }

    public YearRecapShareRatingSummaryDto? RideRatings { get; set; }

    public List<YearRecapShareParkDto> MostVisitedParks { get; set; } = new();

    public YearRecapShareHighlightDto? MostRepeatedItem { get; set; }

    public YearRecapShareHighlightDto? TopRatedItem { get; set; }

    public YearRecapShareTrendDto? RatingEvolution { get; set; }

    public List<YearRecapShareHighlightDto> NowClosedItems { get; set; } = new();

    public string? PublicCaption { get; set; }

    public bool HasIncompleteCatalog { get; set; }

    public string CalculationVersion { get; set; } = string.Empty;

    public bool IsEmpty { get; set; }
}
