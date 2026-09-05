namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportGlobalItemActivity(
    string ParkItemId,
    string ParkId,
    long CompletedRideCount);
