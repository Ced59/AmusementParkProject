using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripAdmissionFenceWriteResult(
    TripAdmissionWriteOutcome Outcome,
    TripMemberAdmissionFence? Fence = null);
