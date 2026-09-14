namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitSearchMemberCriteriaDto
{
    public int? HeightCentimeters { get; set; }

    public int? MinimumAgeYears { get; set; }

    public int? MaximumAgeYears { get; set; }

    public bool? CanBeAccompanied { get; set; }

    public int? CompanionMinimumAgeYears { get; set; }

    public int? CompanionMaximumAgeYears { get; set; }
}
