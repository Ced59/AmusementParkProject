namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record YearRecapShareParkResult(
    string Name,
    long VisitCount,
    long? CompletedRideCount);
