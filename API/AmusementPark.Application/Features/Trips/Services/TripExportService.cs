using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripExportService
{
    public const string SchemaVersion = "trip-plan-export-v1";
    public const int MaximumExportRequestIdLength = 200;

    private readonly ITripPlanRepository plans;
    private readonly ITripItemDecisionRepository decisions;
    private readonly IParkItemRepository parkItems;
    private readonly IParkRepository parks;
    private readonly TripProgramResultFactory programFactory;
    private readonly TripActivityRecorder activityRecorder;
    private readonly TimeProvider timeProvider;

    public TripExportService(
        ITripPlanRepository plans,
        ITripItemDecisionRepository decisions,
        IParkItemRepository parkItems,
        IParkRepository parks,
        TripProgramResultFactory programFactory,
        TripActivityRecorder activityRecorder,
        TimeProvider? timeProvider = null)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        this.parkItems = parkItems ?? throw new ArgumentNullException(nameof(parkItems));
        this.parks = parks ?? throw new ArgumentNullException(nameof(parks));
        this.programFactory = programFactory ?? throw new ArgumentNullException(nameof(programFactory));
        this.activityRecorder = activityRecorder ?? throw new ArgumentNullException(nameof(activityRecorder));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<TripExportResult>> ExportAsync(
        string userId,
        string tripPlanId,
        string exportRequestId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(
                userId,
                tripPlanId,
                exportRequestId,
                out string normalizedUserId,
                out TripPlanId parsedTripId,
                out string normalizedRequestId))
        {
            return ApplicationResult<TripExportResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        TripPlan? trip = await this.plans.GetAccessibleAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        TripEffectiveRole? role = trip?.ResolveRole(normalizedUserId);
        if (trip is null
            || !role.HasValue
            || !TripAuthorizationPolicy.HasPermission(role.Value, TripPermission.Export))
        {
            return ApplicationResult<TripExportResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        ApplicationResult<TripProgramSnapshotResult> programResult =
            await this.programFactory.BuildSnapshotAsync(parsedTripId, cancellationToken);
        if (!programResult.IsSuccess || programResult.Value is null)
        {
            return ApplicationResult<TripExportResult>.Failure(programResult.Errors);
        }

        IReadOnlyCollection<TripItemDecision> tripDecisions = await this.decisions.ListAsync(
            parsedTripId,
            cancellationToken);
        IReadOnlyCollection<TripExportDecisionResult> exportedDecisions = await this.BuildDecisionsAsync(
            tripDecisions,
            programResult.Value.Parks,
            cancellationToken);
        DateTime generatedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        TripExportResult export = BuildResult(
            trip,
            programResult.Value.Program,
            exportedDecisions,
            generatedAtUtc);

        TripActivityWrite activity = this.activityRecorder.CreateWrite(
            trip,
            normalizedUserId,
            TripActivityKind.PlanExported,
            TripActivityRecorder.IdempotentOperationKey(
                TripActivityKind.PlanExported,
                $"{normalizedUserId}:{normalizedRequestId}"),
            1);
        if (!await this.activityRecorder.PublishReadOnlyAsync(activity, cancellationToken))
        {
            return ApplicationResult<TripExportResult>.Failure(
                TripPlanApplicationErrors.ExportUnavailable());
        }

        return ApplicationResult<TripExportResult>.Success(export);
    }

    private async Task<IReadOnlyCollection<TripExportDecisionResult>> BuildDecisionsAsync(
        IReadOnlyCollection<TripItemDecision> tripDecisions,
        IReadOnlyCollection<Park> knownParks,
        CancellationToken cancellationToken)
    {
        string[] itemIds = tripDecisions.Select(static decision => decision.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<ParkItem> resolvedItems = itemIds.Length == 0
            ? Array.Empty<ParkItem>()
            : await this.parkItems.GetByIdsAsync(itemIds, cancellationToken);
        Dictionary<string, ParkItem> itemsById = resolvedItems
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
            .ToDictionary(static item => item.Id!, StringComparer.Ordinal);
        string[] missingParkIds = resolvedItems.Select(static item => item.ParkId)
            .Where(parkId => knownParks.All(park => !string.Equals(park.Id, parkId, StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<Park> missingParks = missingParkIds.Length == 0
            ? Array.Empty<Park>()
            : await this.parks.GetByIdsAsync(missingParkIds, cancellationToken);
        Dictionary<string, Park> parksById = knownParks.Concat(missingParks)
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .GroupBy(static park => park.Id!, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

        return tripDecisions.OrderBy(static decision => decision.UpdatedAtUtc)
            .Select(decision =>
            {
                ParkItem? item = itemsById.GetValueOrDefault(decision.ParkItemId);
                Park? park = item is null ? null : parksById.GetValueOrDefault(item.ParkId);
                bool itemAvailable = item?.IsVisible == true && park?.IsPubliclyDiscoverable() == true;
                return new TripExportDecisionResult(
                    itemAvailable ? park!.Name?.Trim() : null,
                    itemAvailable ? item!.Name.Trim() : null,
                    itemAvailable,
                    decision.Status,
                    decision.Reason,
                    decision.UpdatedAtUtc);
            })
            .ToArray();
    }

    private static TripExportResult BuildResult(
        TripPlan trip,
        TripProgramResult program,
        IReadOnlyCollection<TripExportDecisionResult> decisions,
        DateTime generatedAtUtc)
    {
        return new TripExportResult(
            SchemaVersion,
            trip.Title,
            new TripDateProposalResult(
                trip.DateProposal.Kind,
                trip.DateProposal.StartDate,
                trip.DateProposal.EndDate,
                trip.DateProposal.CandidateDates),
            trip.DestinationTimeZoneId,
            trip.Status,
            generatedAtUtc,
            program.Candidates.OrderBy(static candidate => candidate.SortPosition)
                .Select(static candidate => new TripExportCandidateResult(
                    candidate.ParkName,
                    candidate.IsParkAvailable,
                    candidate.CandidateDates,
                    candidate.State,
                    candidate.CollectiveNote))
                .ToArray(),
            program.Days.OrderBy(static day => day.LocalDate)
                .Select(static day => new TripExportDayResult(
                    day.LocalDate,
                    day.ParkName,
                    day.IsParkAvailable,
                    day.DesiredArrivalTime,
                    day.GroupNote,
                    day.Blocks.OrderBy(static block => block.SortPosition)
                        .Select(static block => new TripExportDayBlockResult(
                            block.Type,
                            block.Title,
                            block.Details,
                            block.LocalTime))
                        .ToArray()))
                .ToArray(),
            decisions);
    }

    private static bool TryNormalize(
        string userId,
        string tripPlanId,
        string exportRequestId,
        out string normalizedUserId,
        out TripPlanId parsedTripId,
        out string normalizedRequestId)
    {
        normalizedUserId = string.Empty;
        parsedTripId = default;
        normalizedRequestId = exportRequestId?.Trim() ?? string.Empty;
        if (normalizedRequestId.Length is 0 or > MaximumExportRequestIdLength)
        {
            return false;
        }

        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            return TripPlanId.TryParse(tripPlanId, out parsedTripId);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
