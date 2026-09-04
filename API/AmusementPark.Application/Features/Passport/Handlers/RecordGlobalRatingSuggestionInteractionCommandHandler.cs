using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class RecordGlobalRatingSuggestionInteractionCommandHandler
    : ICommandHandler<
        RecordGlobalRatingSuggestionInteractionCommand,
        ApplicationResult<GlobalRatingSuggestionPreferenceResult>>
{
    private readonly IGlobalRatingSuggestionStateRepository stateRepository;
    private readonly IGlobalRatingSuggestionFeatureGate featureGate;
    private readonly IRatingRepository ratingRepository;
    private readonly IPassportClock clock;
    private readonly GlobalRatingSuggestionPolicy policy;

    public RecordGlobalRatingSuggestionInteractionCommandHandler(
        IGlobalRatingSuggestionStateRepository stateRepository,
        IGlobalRatingSuggestionFeatureGate featureGate,
        IRatingRepository ratingRepository,
        IPassportClock clock,
        GlobalRatingSuggestionPolicy policy)
    {
        this.stateRepository = stateRepository;
        this.featureGate = featureGate;
        this.ratingRepository = ratingRepository;
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
            || !Enum.IsDefined(command.InteractionType)
            || command.InteractionType == GlobalRatingSuggestionInteractionType.Presented)
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
        if (!this.IsCurrentPresentation(state))
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
        bool isIdempotent = IsSameResolvedInteraction(
            refreshedState,
            command.InteractionType);
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
