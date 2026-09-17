using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripDateProposalResult(
    TripDateProposalKind Kind,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyCollection<DateOnly> CandidateDates);
