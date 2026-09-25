namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceBatchCreationOperationState(
    string ClientOperationId,
    bool IsCompleted,
    IReadOnlyList<string> ParkItemIds);
