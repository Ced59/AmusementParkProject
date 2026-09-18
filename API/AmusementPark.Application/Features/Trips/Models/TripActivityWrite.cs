using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripActivityWrite(
    TripPlanId TripPlanId,
    TripMemberId? ActorMemberId,
    TripEffectiveRole? ActorRole,
    TripActivityKind Kind,
    string OperationKey,
    int AffectedCount,
    DateTime OccurredAtUtc,
    string? ChildLeaseOperationId = null,
    long? ChildLeaseEpoch = null,
    long? ChildLeaseGeneration = null);
