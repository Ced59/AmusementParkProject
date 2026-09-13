namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportParkAssessmentPointDto
{
    public string VisitId { get; init; } = string.Empty;
    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();
    public double Rating { get; init; }
}
