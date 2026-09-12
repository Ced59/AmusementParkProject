namespace AmusementPark.Core.Domain.Visits;

public sealed record RideOccurrenceOrderPlan(
    IReadOnlyCollection<RideOccurrenceOrderPosition> Changes,
    IReadOnlyCollection<RideOccurrenceOrderGuard> Guards,
    bool WasNormalized);
