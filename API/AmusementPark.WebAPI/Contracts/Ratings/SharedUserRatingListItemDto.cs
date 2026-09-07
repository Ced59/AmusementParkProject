namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class SharedUserRatingListItemDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public string? ParkItemCategory { get; set; }

    public string? ParkItemType { get; set; }

    public double Value { get; set; }
}
