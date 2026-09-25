using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripExportDayBlockResult(
    TripDayBlockType Type,
    string Title,
    string? Details,
    TimeOnly? LocalTime);
