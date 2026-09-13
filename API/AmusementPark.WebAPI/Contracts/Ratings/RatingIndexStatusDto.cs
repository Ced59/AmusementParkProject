namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingIndexStatusDto
{
    public string Collection { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsPresent { get; set; }

    public bool IsUnique { get; set; }

    public bool IsHidden { get; set; }

    public bool HasUnexpectedOptions { get; set; }

    public bool SupportsExpectedQueries { get; set; }

    public bool MatchesExpectedDefinition { get; set; }

    public string ExpectedKeys { get; set; } = string.Empty;

    public string? ActualKeys { get; set; }
}
