namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareRatingCandidateDto
{
    public string SelectionKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public double Rating { get; set; }
}
