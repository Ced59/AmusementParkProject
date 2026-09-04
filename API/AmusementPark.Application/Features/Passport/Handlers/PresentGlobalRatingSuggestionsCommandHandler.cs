using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class PresentGlobalRatingSuggestionsCommandHandler
    : ICommandHandler<
        PresentGlobalRatingSuggestionsCommand,
        ApplicationResult<GlobalRatingSuggestionPresentationResult>>
{
    private readonly IGlobalRatingSuggestionSourceReader sourceReader;
    private readonly IGlobalRatingSuggestionStateRepository stateRepository;
    private readonly IGlobalRatingSuggestionFeatureGate featureGate;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IPassportClock clock;
    private readonly GlobalRatingSuggestionPolicy policy;

    public PresentGlobalRatingSuggestionsCommandHandler(
        IGlobalRatingSuggestionSourceReader sourceReader,
        IGlobalRatingSuggestionStateRepository stateRepository,
        IGlobalRatingSuggestionFeatureGate featureGate,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IPassportClock clock,
        GlobalRatingSuggestionPolicy policy)
    {
        this.sourceReader = sourceReader;
        this.stateRepository = stateRepository;
        this.featureGate = featureGate;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.clock = clock;
        this.policy = policy;
    }

    public async Task<ApplicationResult<GlobalRatingSuggestionPresentationResult>> HandleAsync(
        PresentGlobalRatingSuggestionsCommand command,
        CancellationToken cancellationToken = default)
    {
        string userId;
        GlobalRatingSuggestionTargetKey[] targets;
        try
        {
            userId = IdentifierRules.NormalizeRequired(command.UserId, nameof(command.UserId));
            targets = NormalizeTargets(command.Targets);
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<GlobalRatingSuggestionPresentationResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        if (targets.Length == 0
            || targets.Length > GetGlobalRatingSuggestionsQueryHandler.MaximumSuggestionCount
            || targets.Any(static target => !Enum.IsDefined(target.TargetType))
            || targets.Distinct().Count() != targets.Length)
        {
            return ApplicationResult<GlobalRatingSuggestionPresentationResult>.Failure(
                PassportApplicationErrors.InvalidGlobalRatingSuggestionInteraction());
        }

        if (!this.featureGate.IsEnabled)
        {
            return Success(false, true, Array.Empty<GlobalRatingSuggestionPresentedTargetResult>());
        }

        bool isEnabled = await this.stateRepository.IsEnabledAsync(userId, cancellationToken);
        if (!isEnabled)
        {
            return Success(true, false, Array.Empty<GlobalRatingSuggestionPresentedTargetResult>());
        }

        IReadOnlyCollection<GlobalRatingSuggestionSource> sources =
            await this.sourceReader.ReadAsync(userId, cancellationToken);
        Dictionary<GlobalRatingSuggestionTargetKey, GlobalRatingSuggestionSource> sourcesByTarget =
            sources.ToDictionary(
                static source => new GlobalRatingSuggestionTargetKey(
                    source.TargetType,
                    source.TargetId));
        IReadOnlyCollection<GlobalRatingSuggestionTargetState> states =
            await this.stateRepository.GetStatesAsync(userId, targets, cancellationToken);
        Dictionary<GlobalRatingSuggestionTargetKey, GlobalRatingSuggestionTargetState> statesByTarget =
            states.ToDictionary(
                static state => new GlobalRatingSuggestionTargetKey(
                    state.TargetType,
                    state.TargetId));
        DateTime nowUtc = this.clock.UtcNow;
        DateTime presentedAtUtc = TruncateToMilliseconds(nowUtc);
        List<GlobalRatingSuggestionPresentedTargetResult> presented =
            new List<GlobalRatingSuggestionPresentedTargetResult>(targets.Length);

        foreach (GlobalRatingSuggestionTargetKey target in targets)
        {
            if (!sourcesByTarget.TryGetValue(target, out GlobalRatingSuggestionSource? source))
            {
                continue;
            }

            statesByTarget.TryGetValue(target, out GlobalRatingSuggestionTargetState? state);
            DateTime? currentPresentationAtUtc = state?.LastPresentedAtUtc is DateTime lastPresentedAtUtc
                && this.policy.IsPresentationCurrent(
                    lastPresentedAtUtc,
                    state.IsAwaitingResolution,
                    nowUtc)
                    ? lastPresentedAtUtc
                    : null;
            GlobalRatingSuggestionEvaluation? evaluation = this.policy.Evaluate(
                source.CurrentGlobalRating,
                source.CurrentGlobalRatingUpdatedAtUtc,
                source.Observations,
                new GlobalRatingSuggestionCadence(
                    true,
                    currentPresentationAtUtc.HasValue ? null : state?.LastPresentedAtUtc),
                nowUtc);
            if (evaluation is null)
            {
                continue;
            }

            RatingTargetMetadataResult? metadata = await RatingTargetMetadataResolver.ResolveAsync(
                source.TargetType,
                source.TargetId,
                this.parkRepository,
                this.parkItemRepository,
                cancellationToken);
            if (metadata?.CanReceiveVisitorRatings != true)
            {
                continue;
            }

            if (currentPresentationAtUtc.HasValue)
            {
                presented.Add(new GlobalRatingSuggestionPresentedTargetResult(
                    target.TargetType,
                    target.TargetId,
                    currentPresentationAtUtc.Value));
                continue;
            }

            bool recorded = await this.stateRepository.TryRecordInteractionAsync(
                userId,
                target.TargetType,
                target.TargetId,
                state?.LastPresentedAtUtc,
                GlobalRatingSuggestionInteractionType.Presented,
                presentedAtUtc,
                cancellationToken);
            if (recorded)
            {
                presented.Add(new GlobalRatingSuggestionPresentedTargetResult(
                    target.TargetType,
                    target.TargetId,
                    presentedAtUtc));
                continue;
            }

            IReadOnlyCollection<GlobalRatingSuggestionTargetState> refreshedStates =
                await this.stateRepository.GetStatesAsync(
                    userId,
                    new[] { target },
                    cancellationToken);
            GlobalRatingSuggestionTargetState? refreshedState =
                refreshedStates.SingleOrDefault();
            if (refreshedState?.LastPresentedAtUtc is DateTime refreshedPresentedAtUtc
                && this.policy.IsPresentationCurrent(
                    refreshedPresentedAtUtc,
                    refreshedState.IsAwaitingResolution,
                    nowUtc))
            {
                presented.Add(new GlobalRatingSuggestionPresentedTargetResult(
                    target.TargetType,
                    target.TargetId,
                    refreshedPresentedAtUtc));
            }
        }

        return Success(true, true, presented);
    }

    private static GlobalRatingSuggestionTargetKey[] NormalizeTargets(
        IReadOnlyCollection<GlobalRatingSuggestionTargetKey> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        return targets.Select(static target => new GlobalRatingSuggestionTargetKey(
                target.TargetType,
                IdentifierRules.NormalizeRequired(target.TargetId, nameof(target.TargetId))))
            .ToArray();
    }

    private static ApplicationResult<GlobalRatingSuggestionPresentationResult> Success(
        bool isAvailable,
        bool isEnabled,
        IReadOnlyCollection<GlobalRatingSuggestionPresentedTargetResult> targets)
    {
        return ApplicationResult<GlobalRatingSuggestionPresentationResult>.Success(
            new GlobalRatingSuggestionPresentationResult(isAvailable, isEnabled, targets));
    }

    private static DateTime TruncateToMilliseconds(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
