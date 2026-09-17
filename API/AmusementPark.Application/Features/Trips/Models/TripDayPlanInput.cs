namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripDayPlanInput(
    string ParkCandidateId,
    TimeOnly? DesiredArrivalTime,
    string? GroupNote,
    IReadOnlyCollection<TripDayBlockInput> Blocks);
