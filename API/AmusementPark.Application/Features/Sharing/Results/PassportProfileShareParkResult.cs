namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareParkResult(
    string Name,
    string? CountryCode,
    long VisitCount,
    int FirstVisitYear,
    int LastVisitYear,
    long? CompletedRideCount,
    PassportProfileShareRatingSummaryResult? VisitRatings);
