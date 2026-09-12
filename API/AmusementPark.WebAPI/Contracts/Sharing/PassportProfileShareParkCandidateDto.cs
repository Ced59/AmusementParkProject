namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareParkCandidateDto
{
    public string ParkId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    public long VisitCount { get; set; }
}
