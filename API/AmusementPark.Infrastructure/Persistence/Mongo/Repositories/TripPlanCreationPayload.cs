namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed record TripPlanCreationPayload(
    string OwnerUserId,
    string Title,
    string DateProposalKind,
    string? StartDate,
    string? EndDate,
    IReadOnlyCollection<string> CandidateDates,
    string? DestinationTimeZoneId);
