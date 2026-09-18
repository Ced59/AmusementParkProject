using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripProgramCoherenceService
{
    private readonly ITripPlanRepository plans;
    private readonly TripProgramResultFactory programFactory;
    private readonly IParkRepository parks;
    private readonly IParkOpeningHoursRepository openingHours;
    private readonly ITripItemDecisionRepository decisions;
    private readonly ITripPreferenceRepository preferences;
    private readonly IParkItemRepository parkItems;
    private readonly TripProgramEvidenceBuilder evidenceBuilder;
    private readonly TripProgramAttractionFactBuilder attractionFactBuilder;
    private readonly TripProgramCoherenceEvaluator evaluator;
    private readonly TripProgramCoherenceIssueMapper issueMapper;
    private readonly TimeProvider timeProvider;

    public TripProgramCoherenceService(
        ITripPlanRepository plans,
        TripProgramResultFactory programFactory,
        IParkRepository parks,
        IParkOpeningHoursRepository openingHours,
        ITripItemDecisionRepository decisions,
        ITripPreferenceRepository preferences,
        IParkItemRepository parkItems,
        TripProgramEvidenceBuilder evidenceBuilder,
        TripProgramAttractionFactBuilder attractionFactBuilder,
        TripProgramCoherenceEvaluator evaluator,
        TripProgramCoherenceIssueMapper issueMapper,
        TimeProvider? timeProvider = null)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.programFactory = programFactory ?? throw new ArgumentNullException(nameof(programFactory));
        this.parks = parks ?? throw new ArgumentNullException(nameof(parks));
        this.openingHours = openingHours ?? throw new ArgumentNullException(nameof(openingHours));
        this.decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.parkItems = parkItems ?? throw new ArgumentNullException(nameof(parkItems));
        this.evidenceBuilder = evidenceBuilder ?? throw new ArgumentNullException(nameof(evidenceBuilder));
        this.attractionFactBuilder = attractionFactBuilder
            ?? throw new ArgumentNullException(nameof(attractionFactBuilder));
        this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        this.issueMapper = issueMapper ?? throw new ArgumentNullException(nameof(issueMapper));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<TripProgramCoherenceResult>> GetAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId))
        {
            return ApplicationResult<TripProgramCoherenceResult>.Failure(
                TripPlanApplicationErrors.Invalid(
                    TripPlanErrorCodes.InvalidState,
                    "A valid trip identifier is required."));
        }

        TripPlan? trip = await this.plans.GetAccessibleAsync(normalizedUserId, parsedTripId, cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripProgramCoherenceResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        ApplicationResult<TripProgramResult> programResult = await this.programFactory.BuildAsync(
            parsedTripId,
            cancellationToken);
        if (!programResult.IsSuccess || programResult.Value is null)
        {
            return ApplicationResult<TripProgramCoherenceResult>.Failure(programResult.Errors);
        }

        TripProgramResult program = programResult.Value;
        string[] parkIds = program.Candidates.Select(static candidate => candidate.ParkId)
            .Concat(program.Days.Select(static day => day.ParkId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Task<IReadOnlyCollection<Park>> parksTask = parkIds.Length == 0
            ? Task.FromResult<IReadOnlyCollection<Park>>(Array.Empty<Park>())
            : this.parks.GetByIdsAsync(parkIds, cancellationToken);
        Task<IReadOnlyCollection<ParkOpeningHoursSchedule>> schedulesTask = parkIds.Length == 0
            ? Task.FromResult<IReadOnlyCollection<ParkOpeningHoursSchedule>>(
                Array.Empty<ParkOpeningHoursSchedule>())
            : this.openingHours.GetByParkIdsAsync(parkIds, cancellationToken);
        Task<IReadOnlyCollection<TripItemDecision>> decisionsTask =
            this.decisions.ListAsync(parsedTripId, cancellationToken);
        await Task.WhenAll(parksTask, schedulesTask, decisionsTask);

        IReadOnlyCollection<Park> resolvedParks = await parksTask;
        IReadOnlyCollection<ParkOpeningHoursSchedule> schedules = await schedulesTask;
        IReadOnlyCollection<TripItemDecision> tripDecisions = await decisionsTask;
        string[] decisionItemIds = tripDecisions.Select(static decision => decision.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] activeUserIds = trip.Members
            .Where(static member => member.State == TripMembershipState.Active)
            .Select(static member => member.UserId)
            .ToArray();
        Task<IReadOnlyCollection<ParkItem>> decisionItemsTask = decisionItemIds.Length == 0
            ? Task.FromResult<IReadOnlyCollection<ParkItem>>(Array.Empty<ParkItem>())
            : this.parkItems.GetByIdsAsync(decisionItemIds, cancellationToken);
        Task<IReadOnlyCollection<TripPreferenceCount>> preferenceCountsTask = decisionItemIds.Length == 0
            ? Task.FromResult<IReadOnlyCollection<TripPreferenceCount>>(Array.Empty<TripPreferenceCount>())
            : this.preferences.SummarizeAsync(trip.Id, activeUserIds, decisionItemIds, cancellationToken);
        await Task.WhenAll(decisionItemsTask, preferenceCountsTask);

        IReadOnlyCollection<ParkItem> decisionItems = await decisionItemsTask;
        IReadOnlyCollection<TripPreferenceCount> preferenceCounts = await preferenceCountsTask;
        Dictionary<string, ParkItem> decisionItemsById = decisionItems
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
            .ToDictionary(static item => item.Id!, StringComparer.Ordinal);
        Dictionary<string, Park> parksById = resolvedParks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .ToDictionary(static park => park.Id!, StringComparer.Ordinal);
        Dictionary<string, ParkOpeningHoursSchedule> schedulesByParkId = schedules
            .GroupBy(static schedule => schedule.ParkId, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        TripProgramEvidenceProjection evidence = this.evidenceBuilder.Build(
            program,
            parksById,
            schedulesByParkId);
        IReadOnlyCollection<TripProgramAttractionFact> attractionFacts = this.attractionFactBuilder.Build(
            tripDecisions,
            decisionItemsById,
            parksById,
            preferenceCounts);
        DateTime evaluatedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyCollection<TripProgramCoherenceIssue> issues = this.evaluator.Evaluate(
            trip.DateProposal,
            evidence.DayFacts,
            attractionFacts,
            evaluatedAtUtc);
        IReadOnlyCollection<TripProgramCoherenceIssueResult> mappedIssues = this.issueMapper.Map(
            issues,
            parksById,
            decisionItemsById,
            schedulesByParkId);
        return ApplicationResult<TripProgramCoherenceResult>.Success(new TripProgramCoherenceResult(
            trip.Id.Value,
            trip.Title,
            trip.Version,
            evaluatedAtUtc,
            issues.Count(static issue => issue.Severity == TripProgramCoherenceSeverity.Critical),
            issues.Count(static issue => issue.Severity == TripProgramCoherenceSeverity.Attention),
            issues.Count(static issue => issue.Severity == TripProgramCoherenceSeverity.Information),
            evidence.Days,
            evidence.TravelSegments,
            mappedIssues));
    }

    private static bool TryNormalizeIdentity(
        string userId,
        string tripPlanId,
        out string normalizedUserId,
        out TripPlanId parsedTripId)
    {
        normalizedUserId = string.Empty;
        parsedTripId = default;
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
