namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class CreateParkFitGroupProfileRequestDto
{
    public string Alias { get; set; } = string.Empty;

    public int? HeightCentimeters { get; set; }

    public int? AgeYears { get; set; }

    public bool CanBeAccompanied { get; set; }

    public int? CompanionAgeYears { get; set; }
}
