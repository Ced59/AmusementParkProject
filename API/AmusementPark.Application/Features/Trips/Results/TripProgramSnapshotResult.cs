using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripProgramSnapshotResult(
    TripProgramResult Program,
    IReadOnlyCollection<Park> Parks);
