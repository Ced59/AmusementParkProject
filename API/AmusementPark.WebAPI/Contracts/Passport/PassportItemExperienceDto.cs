namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportItemExperienceDto
{
    public string VisitId { get; init; } = string.Empty;

    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();
}
