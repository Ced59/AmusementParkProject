namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareCountryDto
{
    public string CountryCode { get; set; } = string.Empty;

    public long ParkCount { get; set; }

    public long VisitCount { get; set; }
}
