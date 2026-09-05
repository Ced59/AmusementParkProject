using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Handlers;

/// <summary>
/// Builds the current user's explainable global-rating suggestions.
/// </summary>
public sealed class GetGlobalRatingSuggestionsQueryHandler
    : IQueryHandler<
        GetGlobalRatingSuggestionsQuery,
        ApplicationResult<GlobalRatingSuggestionsResult>>
{
    public const int MaximumSuggestionCount = 3;

    private readonly IGlobalRatingSuggestionSourceReader sourceReader;
    private readonly IGlobalRatingSuggestionStateRepository stateRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IPassportClock clock;
    private readonly GlobalRatingSuggestionPolicy policy;

    public GetGlobalRatingSuggestionsQueryHandler(
        IGlobalRatingSuggestionSourceReader sourceReader,
        IGlobalRatingSuggestionStateRepository stateRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IPassportClock clock,
        GlobalRatingSuggestionPolicy policy)
    {
        this.sourceReader = sourceReader;
        this.stateRepository = stateRepository;
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
                metadata.ParkItemCategory,
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
