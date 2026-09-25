using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripExportResult(
    string SchemaVersion,
    string Title,
    TripDateProposalResult DateProposal,
    string? DestinationTimeZoneId,
    TripPlanStatus Status,
    DateTime GeneratedAtUtc,
    IReadOnlyCollection<TripExportCandidateResult> CandidateParks,
    IReadOnlyCollection<TripExportDayResult> Days,
    IReadOnlyCollection<TripExportDecisionResult> CollectiveDecisions);
