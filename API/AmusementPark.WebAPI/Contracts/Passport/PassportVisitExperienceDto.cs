namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportVisitExperienceDto
{
    public string VisitId { get; init; } = string.Empty;
    public string ParkId { get; init; } = string.Empty;
    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();
}
