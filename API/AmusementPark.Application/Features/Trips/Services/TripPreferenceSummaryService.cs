using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripPreferenceSummaryService
{
    private const string NeutralDisplayName = "—";
    private readonly ITripPlanRepository plans;
    private readonly ITripPreferenceRepository preferences;
    private readonly ITripItemDecisionRepository decisions;
    private readonly TripEligibleItemReader eligibleItemReader;
    private readonly IImageRepository images;
    private readonly IUserRepository users;
    private readonly TripChildMutationExecutor mutationExecutor;
    private readonly TimeProvider timeProvider;

    public TripPreferenceSummaryService(
        ITripPlanRepository plans,
        ITripPreferenceRepository preferences,
        ITripItemDecisionRepository decisions,
        TripEligibleItemReader eligibleItemReader,
        IImageRepository images,
        IUserRepository users,
        TripChildMutationExecutor mutationExecutor,
        TimeProvider? timeProvider = null)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        this.eligibleItemReader = eligibleItemReader ?? throw new ArgumentNullException(nameof(eligibleItemReader));
        this.images = images ?? throw new ArgumentNullException(nameof(images));
        this.users = users ?? throw new ArgumentNullException(nameof(users));
        this.mutationExecutor = mutationExecutor ?? throw new ArgumentNullException(nameof(mutationExecutor));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<TripPreferenceSummaryResult>> GetAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId))
        {
            return Invalid("A valid trip identifier is required.");
        }

        TripPlan? trip = await this.plans.GetAccessibleAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        return trip is null
            ? ApplicationResult<TripPreferenceSummaryResult>.Failure(TripPlanApplicationErrors.NotFound())
            : await this.BuildAsync(trip, normalizedUserId, cancellationToken);
    }

    public async Task<ApplicationResult<TripPreferenceSummaryResult>> SetDecisionAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        TripItemDecisionInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId)
            || expectedPlanVersion < 1
            || !Enum.IsDefined(input.Status)
            || input.ExpectedDecisionVersion is < 1)
        {
            return Invalid("A valid trip, version and decision are required.");
        }

        TripItemDecisionInput normalizedInput;
        try
        {
            normalizedInput = input with
            {
                ParkItemId = IdentifierRules.NormalizeRequired(input.ParkItemId, nameof(input.ParkItemId)),
                Reason = input.Reason?.Trim() ?? string.Empty,
            };
        }
        catch (ArgumentException)
        {
            return Invalid("A valid attraction identifier is required.");
        }

        TripPlan? trip = await this.plans.GetAccessibleAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripPreferenceSummaryResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        if (trip.Version != expectedPlanVersion)
        {
            return ApplicationResult<TripPreferenceSummaryResult>.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(trip.Version));
        }

        TripEffectiveRole role = trip.ResolveRole(normalizedUserId)!.Value;
        if (!TripAuthorizationPolicy.HasPermission(role, TripPermission.EditProgram))
        {
            return ApplicationResult<TripPreferenceSummaryResult>.Failure(
                TripPlanApplicationErrors.DecisionForbidden());
        }

        return await this.mutationExecutor.ExecuteAccessibleAsync(
            trip,
            normalizedUserId,
            TripPermission.EditProgram,
            Guid.NewGuid().ToString("N"),
            async lease => await this.WriteDecisionAsync(
                trip,
                normalizedUserId,
                normalizedInput,
                lease,
                cancellationToken),
            cancellationToken);
    }

    private async Task<ApplicationResult<TripPreferenceSummaryResult>> WriteDecisionAsync(
        TripPlan trip,
        string actorUserId,
        TripItemDecisionInput input,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        EligibleTripItems eligible = await this.eligibleItemReader.LoadAsync(trip.Id, cancellationToken);
        if (!eligible.ItemsById.ContainsKey(input.ParkItemId))
        {
            return ApplicationResult<TripPreferenceSummaryResult>.Failure(
                TripPlanApplicationErrors.PreferenceItemNotAvailable());
        }

        TripItemDecision? current = await this.decisions.GetAsync(
            trip.Id,
            input.ParkItemId,
            cancellationToken);
        if (current is null && input.ExpectedDecisionVersion.HasValue)
        {
            return ApplicationResult<TripPreferenceSummaryResult>.Failure(
                TripPlanApplicationErrors.DecisionChangedConcurrently(null));
        }

        TripItemDecision decision;
        long? expectedVersion = current?.Version;
        try
        {
            if (current is null)
            {
                decision = TripItemDecision.Create(
                    TripItemDecisionId.New(),
                    trip.Id,
                    input.ParkItemId,
                    input.Status,
                    input.Reason,
                    actorUserId,
                    this.NowUtc());
            }
            else
            {
                if (input.ExpectedDecisionVersion != current.Version)
                {
                    if (current.Status == input.Status
                        && string.Equals(current.Reason, input.Reason, StringComparison.Ordinal))
                    {
                        return await this.BuildAsync(trip, actorUserId, cancellationToken, eligible);
                    }

                    return ApplicationResult<TripPreferenceSummaryResult>.Failure(
                        TripPlanApplicationErrors.DecisionChangedConcurrently(current.Version));
                }

                decision = TripItemDecision.Restore(
                    current.Id,
                    current.TripPlanId,
                    current.ParkItemId,
                    current.Status,
                    current.Reason,
                    current.DecidedByUserId,
                    current.Version,
                    current.CreatedAtUtc,
                    current.UpdatedAtUtc);
                decision.Set(input.Status, input.Reason, actorUserId, this.NowUtc());
                if (decision.Version == current.Version)
                {
                    return await this.BuildAsync(trip, actorUserId, cancellationToken, eligible);
                }
            }
        }
        catch (TripPlanValidationException exception)
        {
            return Invalid(exception.Message, exception.Code);
        }

        TripItemDecisionWriteResult written = expectedVersion.HasValue
            ? await this.decisions.ReplaceAsync(decision, expectedVersion.Value, lease, cancellationToken)
            : await this.decisions.CreateAsync(decision, lease, cancellationToken);
        if (written.Outcome != TripChildWriteOutcome.Success)
        {
            return ApplicationResult<TripPreferenceSummaryResult>.Failure(
                TripPlanApplicationErrors.DecisionChangedConcurrently(written.CurrentVersion));
        }

        return await this.BuildAsync(trip, actorUserId, cancellationToken, eligible);
    }

    private async Task<ApplicationResult<TripPreferenceSummaryResult>> BuildAsync(
        TripPlan trip,
        string currentUserId,
        CancellationToken cancellationToken,
        EligibleTripItems? loadedEligible = null)
    {
        EligibleTripItems eligible = loadedEligible
            ?? await this.eligibleItemReader.LoadAsync(trip.Id, cancellationToken);
        TripMember[] activeMembers = trip.Members
            .Where(static member => member.State == TripMembershipState.Active)
            .ToArray();
        string[] itemIds = eligible.OrderedItems.Select(static item => item.Id).ToArray();
        string[] activeUserIds = activeMembers.Select(static member => member.UserId).ToArray();
        IReadOnlyCollection<TripPreferenceCount> counts = await this.preferences.SummarizeAsync(
            trip.Id,
            activeUserIds,
            itemIds,
            cancellationToken);
        Dictionary<string, Dictionary<TripItemPreferenceLevel, int>> countsByItem = counts
            .GroupBy(static count => count.ParkItemId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToDictionary(
                    static count => count.Level,
                    static count => count.Count),
                StringComparer.Ordinal);
        IReadOnlyCollection<TripItemDecision> decisions = await this.decisions.ListAsync(
            trip.Id,
            cancellationToken);
        Dictionary<string, TripItemDecision> decisionsByItem = decisions
            .Where(decision => eligible.ItemsById.ContainsKey(decision.ParkItemId))
            .ToDictionary(static decision => decision.ParkItemId, StringComparer.Ordinal);
        string[] authorIds = decisionsByItem.Values
            .Select(static decision => decision.DecidedByUserId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<User> authors = authorIds.Length == 0
            ? Array.Empty<User>()
            : await this.users.GetByIdsAsync(authorIds, cancellationToken);
        IReadOnlyDictionary<string, User> authorsById = authors.ToDictionary(
            static author => author.Id,
            StringComparer.Ordinal);
        IReadOnlyDictionary<string, string> imageIds = await this.images.GetMainImageIdsByOwnersAsync(
            ImageOwnerType.ParkItem,
            itemIds,
            ImageCategory.ParkItem,
            true,
            cancellationToken);

        TripItemPreferenceSummaryResult[] items = eligible.OrderedItems.Select(item =>
        {
            Dictionary<TripItemPreferenceLevel, int>? itemCounts = countsByItem.GetValueOrDefault(item.Id);
            TripPreferenceAggregate aggregate = TripPreferenceAggregate.Create(
                activeMembers.Length,
                GetCount(itemCounts, TripItemPreferenceLevel.MustDo),
                GetCount(itemCounts, TripItemPreferenceLevel.WantToDo),
                GetCount(itemCounts, TripItemPreferenceLevel.Optional),
                GetCount(itemCounts, TripItemPreferenceLevel.NotForMe));
            TripItemDecisionResult? decisionResult = decisionsByItem.TryGetValue(
                item.Id,
                out TripItemDecision? decision)
                ? new TripItemDecisionResult(
                    decision.Status,
                    decision.Reason,
                    ResolveDisplayName(authorsById, decision.DecidedByUserId),
                    decision.UpdatedAtUtc,
                    decision.Version)
                : null;
            return new TripItemPreferenceSummaryResult(
                item.ParkId,
                eligible.ParksById[item.ParkId].Name!.Trim(),
                item.Id,
                item.Name.Trim(),
                imageIds.GetValueOrDefault(item.Id),
                aggregate.MustDoCount,
                aggregate.WantToDoCount,
                aggregate.OptionalCount,
                aggregate.NotForMeCount,
                aggregate.UnansweredCount,
                aggregate.Compatibility,
                aggregate.IsCompatibilityKnown,
                aggregate.HasIndividualConstraint,
                aggregate.IsGroupPriority,
                NormalizeOptional(item.AttractionDetails?.Status),
                NormalizeOptional(item.AttractionDetails?.SourceUrl),
                null,
                decisionResult);
        }).ToArray();
        TripEffectiveRole role = trip.ResolveRole(currentUserId)!.Value;
        return ApplicationResult<TripPreferenceSummaryResult>.Success(new TripPreferenceSummaryResult(
            trip.Id.Value,
            trip.Title,
            trip.Version,
            activeMembers.Length,
            TripAuthorizationPolicy.HasPermission(role, TripPermission.EditProgram),
            items));
    }

    private DateTime NowUtc()
    {
        return this.timeProvider.GetUtcNow().UtcDateTime;
    }

    private static int GetCount(
        IReadOnlyDictionary<TripItemPreferenceLevel, int>? counts,
        TripItemPreferenceLevel level)
    {
        return counts?.GetValueOrDefault(level) ?? 0;
    }

    private static string ResolveDisplayName(IReadOnlyDictionary<string, User> users, string userId)
    {
        return users.TryGetValue(userId, out User? user)
            ? user.ResolvePublicDisplayName() ?? NeutralDisplayName
            : NeutralDisplayName;
    }

    private static string? NormalizeOptional(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
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

    private static ApplicationResult<TripPreferenceSummaryResult> Invalid(
        string message,
        string code = TripPlanErrorCodes.InvalidDecision)
    {
        return ApplicationResult<TripPreferenceSummaryResult>.Failure(
            TripPlanApplicationErrors.Invalid(code, message));
    }
}
