using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripDayBlockInput(
    string? BlockId,
    TripDayBlockType Type,
    string Title,
    string? Details,
    TimeOnly? LocalTime,
    long SortPosition);
