namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareSelectionDto
{
    public List<PassportProfileShareYearCandidateDto> Years { get; set; } = new();

    public List<PassportProfileShareParkCandidateDto> Parks { get; set; } = new();

    public List<PassportProfileShareRatingCandidateDto> Ratings { get; set; } = new();

    public int MaximumSelectedParks { get; set; }

    public List<int>? SavedSelectedYears { get; set; }

    public List<string>? SavedSelectedParkIds { get; set; }

    public List<string>? SavedSelectedRatingKeys { get; set; }

    public string? SavedPublicCaption { get; set; }

    public string SavedVisibility { get; set; } = "Unlisted";

    public bool SavedAllowsComparisons { get; set; }

    public bool HasSavedSnapshot { get; set; }
}
