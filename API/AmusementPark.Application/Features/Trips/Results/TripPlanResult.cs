using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripPlanResult(
    string TripPlanId,
    string Title,
    TripDateProposalResult DateProposal,
    string? DestinationTimeZoneId,
    TripPlanStatus Status,
    TripPlanAccessScope AccessScope,
    int MemberCount,
    bool IsOwner,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    long Version);
