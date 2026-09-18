using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripInvitationCreateInput(
    long ExpectedPlanVersion,
    TripDelegatedRole ProposedRole,
    int LifetimeHours,
    string? TargetEmail);
