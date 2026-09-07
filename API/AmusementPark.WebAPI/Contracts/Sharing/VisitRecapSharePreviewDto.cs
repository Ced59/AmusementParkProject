namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class VisitRecapSharePreviewDto
{
    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public VisitRecapShareDateDto? Date { get; set; }

    public int? DistinctItemCount { get; set; }

    public int? TotalRideCount { get; set; }

    public List<string> Categories { get; set; } = new();

    public double? ParkRating { get; set; }

    public VisitRecapShareHighlightDto? TopRatedItem { get; set; }

    public VisitRecapShareHighlightDto? MostRepeatedItem { get; set; }

    public List<VisitRecapShareItemDto> Items { get; set; } = new();

    public string? PublicCaption { get; set; }

    public bool HasHiddenDate { get; set; }

    public bool HasIncompleteRatings { get; set; }

    public bool HasIncompleteItems { get; set; }
}
