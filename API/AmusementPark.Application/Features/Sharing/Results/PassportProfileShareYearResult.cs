namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareYearResult(
    int Year,
    long VisitCount,
    long ParkCount,
    long? CompletedRideCount);
