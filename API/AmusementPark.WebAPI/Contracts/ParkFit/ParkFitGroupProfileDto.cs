namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitGroupProfileDto
{
    public string ProfileId { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public int? HeightCentimeters { get; set; }

    public int? AgeYears { get; set; }

    public bool CanBeAccompanied { get; set; }

    public int? CompanionAgeYears { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public long Version { get; set; }
}
