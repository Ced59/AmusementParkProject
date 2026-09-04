using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetGlobalRatingSuggestionsQueryHandler
    : IQueryHandler<
        GetGlobalRatingSuggestionsQuery,
        ApplicationResult<GlobalRatingSuggestionsResult>>
{
    public const int MaximumSuggestionCount = 3;

    private readonly IGlobalRatingSuggestionSourceReader sourceReader;
    private readonly IGlobalRatingSuggestionStateRepository stateRepository;
    private readonly IGlobalRatingSuggestionFeatureGate featureGate;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IPassportClock clock;
    private readonly GlobalRatingSuggestionPolicy policy;

    public GetGlobalRatingSuggestionsQueryHandler(
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

    public async Task<ApplicationResult<GlobalRatingSuggestionsResult>> HandleAsync(
        GetGlobalRatingSuggestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        string userId;
        try
        {
            userId = IdentifierRules.NormalizeRequired(query.UserId, nameof(query.UserId));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<GlobalRatingSuggestionsResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        if (!this.featureGate.IsEnabled)
        {
            return ApplicationResult<GlobalRatingSuggestionsResult>.Success(
                CreateEmpty(false, true));
        }

        bool userEnabled = await this.stateRepository.IsEnabledAsync(userId, cancellationToken);
        if (!userEnabled)
        {
            return ApplicationResult<GlobalRatingSuggestionsResult>.Success(
                CreateEmpty(true, false));
        }

        IReadOnlyCollection<GlobalRatingSuggestionSource> sources =
            await this.sourceReader.ReadAsync(userId, cancellationToken);
        GlobalRatingSuggestionTargetKey[] targetKeys = sources
            .Select(static source => new GlobalRatingSuggestionTargetKey(
                source.TargetType,
                source.TargetId))
            .ToArray();
        IReadOnlyCollection<GlobalRatingSuggestionTargetState> states =
            await this.stateRepository.GetStatesAsync(
                userId,
                targetKeys,
                cancellationToken);
        Dictionary<GlobalRatingSuggestionTargetKey, GlobalRatingSuggestionTargetState> statesByTarget =
            states.ToDictionary(
                static state => new GlobalRatingSuggestionTargetKey(
                    state.TargetType,
                    state.TargetId));

        List<(GlobalRatingSuggestionSource Source, GlobalRatingSuggestionEvaluation Evaluation)>
            candidates = new List<(
                GlobalRatingSuggestionSource,
                GlobalRatingSuggestionEvaluation)>();
        foreach (GlobalRatingSuggestionSource source in sources)
        {
            GlobalRatingSuggestionTargetKey key =
                new GlobalRatingSuggestionTargetKey(source.TargetType, source.TargetId);
            statesByTarget.TryGetValue(key, out GlobalRatingSuggestionTargetState? state);
            GlobalRatingSuggestionEvaluation? evaluation = this.policy.Evaluate(
                source.CurrentGlobalRating,
                source.CurrentGlobalRatingUpdatedAtUtc,
                source.Observations,
                new GlobalRatingSuggestionCadence(true, state?.LastPresentedAtUtc),
                this.clock.UtcNow);
            if (evaluation is not null)
            {
                candidates.Add((source, evaluation));
            }
        }

        List<GlobalRatingSuggestionResult> suggestions =
            new List<GlobalRatingSuggestionResult>(MaximumSuggestionCount);
        foreach ((GlobalRatingSuggestionSource source, GlobalRatingSuggestionEvaluation evaluation)
            in candidates
                .OrderByDescending(static candidate => candidate.Evaluation.LatestObservationAtUtc))
        {
            RatingTargetMetadataResult? metadata = await RatingTargetMetadataResolver.ResolveAsync(
                source.TargetType,
                source.TargetId,
                this.parkRepository,
                this.parkItemRepository,
                cancellationToken);
            if (metadata is null || !metadata.CanReceiveVisitorRatings)
            {
                continue;
            }

            suggestions.Add(new GlobalRatingSuggestionResult(
                source.TargetType,
                source.TargetId,
                metadata.TargetName,
                source.ParkId,
                metadata.ParkName,
                source.ParkItemCategory,
                source.CurrentGlobalRating.DoubleValue,
                evaluation.LatestObservation.DoubleValue,
                evaluation.RecentAverage,
                evaluation.HistoricalMedian,
                evaluation.NewObservationCount,
                evaluation.RecentObservationCount,
                evaluation.Reason,
                evaluation.LatestObservationAtUtc));
            if (suggestions.Count == MaximumSuggestionCount)
            {
                break;
            }
        }

        return ApplicationResult<GlobalRatingSuggestionsResult>.Success(
            new GlobalRatingSuggestionsResult(
                true,
                true,
                GlobalRatingSuggestionPolicy.MinimumNewObservationCount,
                (int)GlobalRatingSuggestionPolicy.PresentationCooldown.TotalDays,
                suggestions));
    }

    private static GlobalRatingSuggestionsResult CreateEmpty(bool isAvailable, bool userEnabled)
    {
        return new GlobalRatingSuggestionsResult(
            isAvailable,
            userEnabled,
            GlobalRatingSuggestionPolicy.MinimumNewObservationCount,
            (int)GlobalRatingSuggestionPolicy.PresentationCooldown.TotalDays,
            Array.Empty<GlobalRatingSuggestionResult>());
    }
}

public sealed class SetGlobalRatingSuggestionsEnabledCommandHandler
    : ICommandHandler<
        SetGlobalRatingSuggestionsEnabledCommand,
        ApplicationResult<GlobalRatingSuggestionPreferenceResult>>
{
    private readonly IGlobalRatingSuggestionStateRepository stateRepository;
    private readonly IGlobalRatingSuggestionFeatureGate featureGate;
    private readonly IPassportClock clock;

    public SetGlobalRatingSuggestionsEnabledCommandHandler(
        IGlobalRatingSuggestionStateRepository stateRepository,
        IGlobalRatingSuggestionFeatureGate featureGate,
        IPassportClock clock)
    {
        this.stateRepository = stateRepository;
        this.featureGate = featureGate;
        this.clock = clock;
    }

    public async Task<ApplicationResult<GlobalRatingSuggestionPreferenceResult>> HandleAsync(
        SetGlobalRatingSuggestionsEnabledCommand command,
        CancellationToken cancellationToken = default)
    {
        string userId;
        try
        {
            userId = IdentifierRules.NormalizeRequired(command.UserId, nameof(command.UserId));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        await this.stateRepository.SetEnabledAsync(
            userId,
            command.IsEnabled,
            this.clock.UtcNow,
            cancellationToken);
        return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Success(
            new GlobalRatingSuggestionPreferenceResult(
                this.featureGate.IsEnabled,
                command.IsEnabled));
    }
}

public sealed class RecordGlobalRatingSuggestionInteractionCommandHandler
    : ICommandHandler<
        RecordGlobalRatingSuggestionInteractionCommand,
        ApplicationResult<GlobalRatingSuggestionPreferenceResult>>
{
    private readonly IGlobalRatingSuggestionStateRepository stateRepository;
    private readonly IGlobalRatingSuggestionFeatureGate featureGate;
    private readonly IRatingRepository ratingRepository;
    private readonly IGlobalRatingSuggestionSourceReader sourceReader;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IPassportClock clock;
    private readonly GlobalRatingSuggestionPolicy policy;

    public RecordGlobalRatingSuggestionInteractionCommandHandler(
        IGlobalRatingSuggestionStateRepository stateRepository,
        IGlobalRatingSuggestionFeatureGate featureGate,
        IRatingRepository ratingRepository,
        IGlobalRatingSuggestionSourceReader sourceReader,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IPassportClock clock,
        GlobalRatingSuggestionPolicy policy)
    {
        this.stateRepository = stateRepository;
        this.featureGate = featureGate;
        this.ratingRepository = ratingRepository;
        this.sourceReader = sourceReader;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.clock = clock;
        this.policy = policy;
    }

    public async Task<ApplicationResult<GlobalRatingSuggestionPreferenceResult>> HandleAsync(
        RecordGlobalRatingSuggestionInteractionCommand command,
        CancellationToken cancellationToken = default)
    {
        string userId;
        string targetId;
        try
        {
            userId = IdentifierRules.NormalizeRequired(command.UserId, nameof(command.UserId));
            targetId = IdentifierRules.NormalizeRequired(command.TargetId, nameof(command.TargetId));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        if (!Enum.IsDefined(command.TargetType)
            || !Enum.IsDefined(command.InteractionType))
        {
            return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Failure(
                PassportApplicationErrors.InvalidGlobalRatingSuggestionInteraction());
        }

        if (!this.featureGate.IsEnabled)
        {
            return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Success(
                new GlobalRatingSuggestionPreferenceResult(false, true));
        }

        bool isEnabled = await this.stateRepository.IsEnabledAsync(userId, cancellationToken);
        if (!isEnabled)
        {
            return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Success(
                new GlobalRatingSuggestionPreferenceResult(true, false));
        }

        UserRating? rating = await this.ratingRepository.GetUserRatingAsync(
            userId,
            command.TargetType,
            targetId,
            cancellationToken);
        if (rating is null)
        {
            return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Failure(
                PassportApplicationErrors.GlobalRatingSuggestionTargetNotFound());
        }

        GlobalRatingSuggestionTargetKey key = new GlobalRatingSuggestionTargetKey(
            command.TargetType,
            targetId);
        GlobalRatingSuggestionTargetState? state = await this.ReadStateAsync(
            userId,
            key,
            cancellationToken);
        if (command.InteractionType == GlobalRatingSuggestionInteractionType.Presented)
        {
            bool isEligible = await this.IsEligibleForPresentationAsync(
                userId,
                key,
                state,
                cancellationToken);
            if (!isEligible)
            {
                return this.IsCurrentPresentation(state)
                    ? Success()
                    : InvalidInteraction();
            }
        }
        else if (!this.IsCurrentPresentation(state))
        {
            return IsSameResolvedInteraction(state, command.InteractionType)
                ? Success()
                : InvalidInteraction();
        }

        bool recorded = await this.stateRepository.TryRecordInteractionAsync(
            userId,
            command.TargetType,
            targetId,
            state?.LastPresentedAtUtc,
            command.InteractionType,
            this.clock.UtcNow,
            cancellationToken);
        if (recorded)
        {
            return Success();
        }

        GlobalRatingSuggestionTargetState? refreshedState = await this.ReadStateAsync(
            userId,
            key,
            cancellationToken);
        bool isIdempotent = command.InteractionType == GlobalRatingSuggestionInteractionType.Presented
            ? this.IsCurrentPresentation(refreshedState)
            : IsSameResolvedInteraction(refreshedState, command.InteractionType);
        return isIdempotent ? Success() : InvalidInteraction();
    }

    private async Task<GlobalRatingSuggestionTargetState?> ReadStateAsync(
        string userId,
        GlobalRatingSuggestionTargetKey key,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<GlobalRatingSuggestionTargetState> states =
            await this.stateRepository.GetStatesAsync(
                userId,
                new[] { key },
                cancellationToken);
        return states.SingleOrDefault();
    }

    private async Task<bool> IsEligibleForPresentationAsync(
        string userId,
        GlobalRatingSuggestionTargetKey key,
        GlobalRatingSuggestionTargetState? state,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<GlobalRatingSuggestionSource> sources =
            await this.sourceReader.ReadAsync(userId, cancellationToken);
        GlobalRatingSuggestionSource? source = sources.SingleOrDefault(candidate =>
            candidate.TargetType == key.TargetType
            && string.Equals(candidate.TargetId, key.TargetId, StringComparison.Ordinal));
        if (source is null)
        {
            return false;
        }

        GlobalRatingSuggestionEvaluation? evaluation = this.policy.Evaluate(
            source.CurrentGlobalRating,
            source.CurrentGlobalRatingUpdatedAtUtc,
            source.Observations,
            new GlobalRatingSuggestionCadence(true, state?.LastPresentedAtUtc),
            this.clock.UtcNow);
        if (evaluation is null)
        {
            return false;
        }

        RatingTargetMetadataResult? metadata = await RatingTargetMetadataResolver.ResolveAsync(
            source.TargetType,
            source.TargetId,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken);
        return metadata?.CanReceiveVisitorRatings == true;
    }

    private bool IsCurrentPresentation(GlobalRatingSuggestionTargetState? state)
    {
        return state is not null
            && this.policy.IsPresentationCurrent(
                state.LastPresentedAtUtc,
                state.IsAwaitingResolution,
                this.clock.UtcNow);
    }

    private static bool IsSameResolvedInteraction(
        GlobalRatingSuggestionTargetState? state,
        GlobalRatingSuggestionInteractionType interactionType)
    {
        if (state?.LastPresentedAtUtc is null || state.IsAwaitingResolution)
        {
            return false;
        }

        DateTime? resolvedAtUtc = interactionType switch
        {
            GlobalRatingSuggestionInteractionType.Accepted => state.LastAcceptedAtUtc,
            GlobalRatingSuggestionInteractionType.Dismissed => state.LastDismissedAtUtc,
            _ => null,
        };
        return resolvedAtUtc >= state.LastPresentedAtUtc;
    }

    private static ApplicationResult<GlobalRatingSuggestionPreferenceResult> Success()
    {
        return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Success(
            new GlobalRatingSuggestionPreferenceResult(true, true));
    }

    private static ApplicationResult<GlobalRatingSuggestionPreferenceResult> InvalidInteraction()
    {
        return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Failure(
            PassportApplicationErrors.InvalidGlobalRatingSuggestionInteraction());
    }
}
