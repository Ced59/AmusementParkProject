namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareRatingDto
{
    public string TargetType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public string? Category { get; set; }

    public double Rating { get; set; }
}
