namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class VisitRecapShareItemDto
{
    public string ParkItemId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public int? RideCount { get; set; }

    public double? AverageRating { get; set; }

    public bool IsMissed { get; set; }
}
