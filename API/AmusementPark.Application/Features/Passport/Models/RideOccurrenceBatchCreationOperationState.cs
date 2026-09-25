namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceBatchCreationOperationState(
    string ClientOperationId,
    bool IsCompleted,
    bool IsConflicted,
    IReadOnlyList<string> ParkItemIds,
    RideOccurrenceCreationPreparation? Preparation = null,
    string? ConcurrencyToken = null);
