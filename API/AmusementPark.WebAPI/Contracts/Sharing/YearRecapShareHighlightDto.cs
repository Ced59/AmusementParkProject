namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class YearRecapShareHighlightDto
{
    public string Name { get; set; } = string.Empty;

    public long RideCount { get; set; }

    public long RatingCount { get; set; }

    public double? AverageRating { get; set; }

    public bool IsNowClosed { get; set; }
}
