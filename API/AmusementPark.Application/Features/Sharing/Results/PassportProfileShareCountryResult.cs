namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareCountryResult(
    string CountryCode,
    long ParkCount,
    long VisitCount);
