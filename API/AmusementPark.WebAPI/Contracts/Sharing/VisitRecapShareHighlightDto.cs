namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class VisitRecapShareHighlightDto
{
    public string Name { get; set; } = string.Empty;

    public int? RideCount { get; set; }

    public double? Rating { get; set; }
}
