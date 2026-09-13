namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ProfileComparisonRatingDto
{
    public string TargetType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public string? Category { get; set; }

    public double CreatorRating { get; set; }

    public double AcceptorRating { get; set; }

    public double AbsoluteDifference { get; set; }

    public string Affinity { get; set; } = string.Empty;
}
