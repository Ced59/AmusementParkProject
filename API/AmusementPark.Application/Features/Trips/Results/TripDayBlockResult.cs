using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripDayBlockResult(
    string BlockId,
    TripDayBlockType Type,
    string Title,
    string? Details,
    TimeOnly? LocalTime,
    long SortPosition);
