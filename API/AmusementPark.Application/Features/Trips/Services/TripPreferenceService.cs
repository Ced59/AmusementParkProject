using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripPreferenceService
{
    private readonly ITripPlanRepository tripPlanRepository;
    private readonly ITripPreferenceRepository preferenceRepository;
    private readonly TripEligibleItemReader eligibleItemReader;
    private readonly IImageRepository imageRepository;
    private readonly TripChildMutationExecutor mutationExecutor;
    private readonly TimeProvider timeProvider;
    private readonly TripActivityRecorder? activityRecorder;

    public TripPreferenceService(
        ITripPlanRepository tripPlanRepository,
        ITripPreferenceRepository preferenceRepository,
        TripEligibleItemReader eligibleItemReader,
        IImageRepository imageRepository,
        TripChildMutationExecutor mutationExecutor,
        TimeProvider? timeProvider = null,
        TripActivityRecorder? activityRecorder = null)
    {
        this.tripPlanRepository = tripPlanRepository ?? throw new ArgumentNullException(nameof(tripPlanRepository));
        this.preferenceRepository = preferenceRepository ?? throw new ArgumentNullException(nameof(preferenceRepository));
        this.eligibleItemReader = eligibleItemReader ?? throw new ArgumentNullException(nameof(eligibleItemReader));
        this.imageRepository = imageRepository ?? throw new ArgumentNullException(nameof(imageRepository));
        this.mutationExecutor = mutationExecutor ?? throw new ArgumentNullException(nameof(mutationExecutor));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.activityRecorder = activityRecorder;
    }

    public async Task<ApplicationResult<TripPreferenceBoardResult>> GetAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId))
        {
            return Invalid("A valid trip identifier is required.");
        }

        TripPlan? trip = await this.tripPlanRepository.GetAccessibleAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripPreferenceBoardResult>.Failure(
                TripPlanApplicationErrors.NotFound());
        }

        return await this.BuildBoardAsync(trip, normalizedUserId, cancellationToken);
    }

    public async Task<ApplicationResult<TripPreferenceBoardResult>> SetAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        IReadOnlyCollection<TripItemPreferenceInput> inputs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId)
            || expectedPlanVersion < 1)
        {
            return Invalid("A valid trip identifier and expected version are required.");
        }

        if (inputs.Count is < 1 or > TripItemPreference.MaximumBatchSize)
        {
            return Invalid($"A preference batch must contain between 1 and {TripItemPreference.MaximumBatchSize} entries.");
        }

        if (inputs.Any(static input => !Enum.IsDefined(input.Level)
            || input.Reason.HasValue && !Enum.IsDefined(input.Reason.Value)
            || input.Level == TripItemPreferenceLevel.Unknown && input.Reason.HasValue
            || input.ExpectedPreferenceVersion is < 1))
        {
            return Invalid("The preference level or reason is invalid.");
        }

        TripItemPreferenceInput[] normalizedInputs;
        try
        {
            normalizedInputs = inputs.Select(static input => new TripItemPreferenceInput(
                    IdentifierRules.NormalizeRequired(input.ParkItemId, nameof(input.ParkItemId)),
                    input.ExpectedPreferenceVersion,
                    input.Level,
                    input.Level == TripItemPreferenceLevel.Unknown ? null : input.Reason))
                .ToArray();
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }

        if (normalizedInputs.Select(static input => input.ParkItemId)
            .Distinct(StringComparer.Ordinal).Count() != normalizedInputs.Length)
        {
            return Invalid("A preference batch cannot contain the same attraction twice.");
        }

        TripPlan? trip = await this.tripPlanRepository.GetAccessibleAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripPreferenceBoardResult>.Failure(
                TripPlanApplicationErrors.NotFound());
        }

        if (trip.Version != expectedPlanVersion)
        {
            return ApplicationResult<TripPreferenceBoardResult>.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(trip.Version));
        }

        return await this.mutationExecutor.ExecuteAccessibleAsync(
            trip,
            normalizedUserId,
            TripPermission.Vote,
            Guid.NewGuid().ToString("N"),
            async lease => await this.WriteAsync(
                trip,
                normalizedUserId,
                normalizedInputs,
                lease,
                cancellationToken),
            cancellationToken);
    }

    private async Task<ApplicationResult<TripPreferenceBoardResult>> WriteAsync(
        TripPlan trip,
        string userId,
        IReadOnlyCollection<TripItemPreferenceInput> inputs,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        TripMember actor = trip.Members.Single(member => member.State == TripMembershipState.Active
            && string.Equals(member.UserId, userId, StringComparison.Ordinal));
        EligibleTripItems eligible = await this.eligibleItemReader.LoadAsync(trip.Id, cancellationToken);
        if (inputs.Any(input => !eligible.ItemsById.ContainsKey(input.ParkItemId)))
        {
            return ApplicationResult<TripPreferenceBoardResult>.Failure(
                TripPlanApplicationErrors.PreferenceItemNotAvailable());
        }

        IReadOnlyCollection<TripItemPreference> stored = await this.preferenceRepository.ListForUserAsync(
            trip.Id,
            userId,
            cancellationToken);
        Dictionary<string, TripItemPreference> preferences = stored.ToDictionary(
            static preference => preference.ParkItemId,
            StringComparer.Ordinal);
        int newPreferenceCount = inputs.Count(input => !preferences.ContainsKey(input.ParkItemId));
        if (preferences.Count + newPreferenceCount > TripItemPreference.MaximumPreferencesPerMember)
        {
            return Invalid($"A trip member cannot store more than {TripItemPreference.MaximumPreferencesPerMember} preferences.");
        }

        List<(TripItemPreference Preference, long? ExpectedVersion)> mutations = new();
        foreach (TripItemPreferenceInput input in inputs)
        {
            if (preferences.TryGetValue(input.ParkItemId, out TripItemPreference? current))
            {
                if (input.ExpectedPreferenceVersion != current.Version)
                {
                    if (current.Level == input.Level
                        && current.Reason == (input.Level == TripItemPreferenceLevel.Unknown ? null : input.Reason))
                    {
                        continue;
                    }

                    return ApplicationResult<TripPreferenceBoardResult>.Failure(
                        TripPlanApplicationErrors.PreferenceChangedConcurrently(current.Version));
                }

                long expectedVersion = current.Version;
                TripItemPreference changed = TripItemPreference.Restore(
                    current.Id,
                    current.TripPlanId,
                    current.MemberId,
                    current.UserId,
                    current.ParkItemId,
                    current.Level,
                    current.Reason,
                    current.Version,
                    current.CreatedAtUtc,
                    current.UpdatedAtUtc);
                try
                {
                    changed.Set(input.Level, input.Reason, this.NowUtc());
                }
                catch (TripPlanValidationException exception)
                {
                    return Invalid(exception.Message, exception.Code);
                }

                if (changed.Version == expectedVersion)
                {
                    continue;
                }

                mutations.Add((changed, expectedVersion));
                continue;
            }

            if (input.ExpectedPreferenceVersion.HasValue)
            {
                return ApplicationResult<TripPreferenceBoardResult>.Failure(
                    TripPlanApplicationErrors.PreferenceChangedConcurrently(null));
            }

            TripItemPreference created;
            try
            {
                created = TripItemPreference.Create(
                    TripItemPreferenceId.New(),
                    trip.Id,
                    actor.Id,
                    userId,
                    input.ParkItemId,
                    input.Level,
                    input.Reason,
                    this.NowUtc());
            }
            catch (TripPlanValidationException exception)
            {
                return Invalid(exception.Message, exception.Code);
            }

            mutations.Add((created, null));
        }

        string activityOperationKey = TripActivityRecorder.ChildOperationKey(
            TripActivityKind.PreferencesUpdated,
            lease);
        TripActivityWrite? batchActivity = mutations.Count == 0
            ? null
            : this.activityRecorder?.CreateWrite(
                trip.Id,
                actor.Id,
                trip.ResolveRole(userId),
                TripActivityKind.PreferencesUpdated,
                activityOperationKey,
                mutations.Count,
                lease);
        int committedCount = 0;
        foreach ((TripItemPreference preference, long? expectedVersion) in mutations)
        {
            TripActivityWrite? pendingActivity = batchActivity is null
                ? null
                : batchActivity with { AffectedCount = 1 };
            TripItemPreferenceWriteResult written = expectedVersion.HasValue
                ? await this.preferenceRepository.ReplaceAsync(
                    preference,
                    expectedVersion.Value,
                    lease,
                    pendingActivity,
                    cancellationToken)
                : await this.preferenceRepository.CreateAsync(
                    preference,
                    lease,
                    pendingActivity,
                    cancellationToken);
            if (written.Outcome != TripChildWriteOutcome.Success || written.Preference is null)
            {
                if (committedCount > 0 && batchActivity is not null)
                {
                    await this.activityRecorder!.PublishAsync(
                        batchActivity with { AffectedCount = committedCount },
                        CancellationToken.None);
                }

                return ApplicationResult<TripPreferenceBoardResult>.Failure(
                    TripPlanApplicationErrors.PreferenceChangedConcurrently(written.CurrentVersion));
            }

            preferences[preference.ParkItemId] = written.Preference;
            committedCount++;
        }

        if (batchActivity is not null)
        {
            await this.activityRecorder!.PublishAsync(
                batchActivity with { AffectedCount = committedCount },
                CancellationToken.None);
        }

        return await this.BuildBoardAsync(trip, userId, cancellationToken, eligible);
    }

    private async Task<ApplicationResult<TripPreferenceBoardResult>> BuildBoardAsync(
        TripPlan trip,
        string userId,
        CancellationToken cancellationToken,
        EligibleTripItems? loadedEligible = null)
    {
        EligibleTripItems eligible = loadedEligible
            ?? await this.eligibleItemReader.LoadAsync(trip.Id, cancellationToken);
        IReadOnlyCollection<TripItemPreference> preferences = await this.preferenceRepository.ListForUserAsync(
            trip.Id,
            userId,
            cancellationToken);
        Dictionary<string, TripItemPreference> preferencesByItem = preferences.ToDictionary(
            static preference => preference.ParkItemId,
            StringComparer.Ordinal);
        string[] itemIds = eligible.OrderedItems.Select(static item => item.Id).ToArray();
        IReadOnlyDictionary<string, string> imageIds = await this.imageRepository.GetMainImageIdsByOwnersAsync(
            ImageOwnerType.ParkItem,
            itemIds,
            ImageCategory.ParkItem,
            true,
            cancellationToken);
        TripEffectiveRole role = trip.ResolveRole(userId)!.Value;
        TripItemPreferenceResult[] items = eligible.OrderedItems.Select(item =>
        {
            preferencesByItem.TryGetValue(item.Id, out TripItemPreference? preference);
            return new TripItemPreferenceResult(
                item.ParkId,
                eligible.ParksById[item.ParkId].Name!.Trim(),
                item.Id,
                item.Name.Trim(),
                imageIds.GetValueOrDefault(item.Id),
                preference?.Level ?? TripItemPreferenceLevel.Unknown,
                preference?.Reason,
                preference?.Version);
        }).ToArray();
        return ApplicationResult<TripPreferenceBoardResult>.Success(new TripPreferenceBoardResult(
            trip.Id.Value,
            trip.Title,
            trip.Version,
            TripAuthorizationPolicy.HasPermission(role, TripPermission.Vote),
            items));
    }

    private DateTime NowUtc()
    {
        return this.timeProvider.GetUtcNow().UtcDateTime;
    }

    private static bool TryNormalizeIdentity(
        string userId,
        string tripPlanId,
        out string normalizedUserId,
        out TripPlanId parsedTripId)
    {
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            return TripPlanId.TryParse(tripPlanId, out parsedTripId);
        }
        catch (ArgumentException)
        {
            normalizedUserId = string.Empty;
            parsedTripId = default;
            return false;
        }
    }

    private static ApplicationResult<TripPreferenceBoardResult> Invalid(
        string message,
        string code = TripPlanErrorCodes.InvalidPreference)
    {
        return ApplicationResult<TripPreferenceBoardResult>.Failure(
            TripPlanApplicationErrors.Invalid(code, message));
    }

}
