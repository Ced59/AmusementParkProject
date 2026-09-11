namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareInputDto
{
    public List<int>? SelectedYears { get; set; }

    public List<string>? SelectedParkIds { get; set; }

    public List<string>? SelectedRatingKeys { get; set; }

    public string? PublicCaption { get; set; }

    public string Visibility { get; set; } = "Unlisted";

    public bool AllowsComparisons { get; set; }
}
