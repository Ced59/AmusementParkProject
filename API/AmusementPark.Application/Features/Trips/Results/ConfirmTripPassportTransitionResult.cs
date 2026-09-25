namespace AmusementPark.Application.Features.Trips.Results;

public sealed record ConfirmTripPassportTransitionResult(
    string VisitId,
    bool WasReplayed,
    int AddedRideCount);
