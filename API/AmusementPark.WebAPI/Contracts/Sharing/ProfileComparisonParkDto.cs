namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ProfileComparisonParkDto
{
    public string Name { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    public long? CreatorVisitCount { get; set; }

    public long? AcceptorVisitCount { get; set; }
}
