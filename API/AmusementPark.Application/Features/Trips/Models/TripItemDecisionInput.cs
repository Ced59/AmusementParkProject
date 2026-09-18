using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripItemDecisionInput(
    string ParkItemId,
    long? ExpectedDecisionVersion,
    TripItemDecisionStatus Status,
    string Reason);
