using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripParticipantResult(
    string MemberId,
    string DisplayName,
    TripEffectiveRole Role,
    bool IsCurrentUser,
    DateTime JoinedAtUtc);
