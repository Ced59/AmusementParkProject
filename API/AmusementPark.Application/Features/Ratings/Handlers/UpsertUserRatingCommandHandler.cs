using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class UpsertUserRatingCommandHandler : ICommandHandler<UpsertUserRatingCommand, ApplicationResult<UserRatingResult>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IRatingRankProvider ratingRankProvider;
    private readonly IRatingRankingMutationGuard rankingMutationGuard;

    public UpsertUserRatingCommandHandler(
        IRatingRepository ratingRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IRatingRankProvider ratingRankProvider,
        IRatingRankingMutationGuard rankingMutationGuard)
    {
        this.ratingRepository = ratingRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.ratingRankProvider = ratingRankProvider;
        this.rankingMutationGuard = rankingMutationGuard;
    }

    public async Task<ApplicationResult<UserRatingResult>> HandleAsync(UpsertUserRatingCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<UserRatingResult>.Failure(ApplicationErrors.Required(nameof(command.UserId)));
        }

        if (string.IsNullOrWhiteSpace(command.TargetId))
        {
            return ApplicationResult<UserRatingResult>.Failure(ApplicationErrors.Required(nameof(command.TargetId)));
        }

        if (!Enum.IsDefined(command.TargetType))
        {
            return ApplicationResult<UserRatingResult>.Failure(RatingApplicationErrors.InvalidTargetType());
        }

        if (!RatingScoreCalculator.IsValidUserRating(command.Value))
        {
            return ApplicationResult<UserRatingResult>.Failure(RatingApplicationErrors.InvalidRatingValue());
        }

        RatingTargetMetadataResult? metadata = await RatingTargetMetadataResolver.ResolveAsync(
            command.TargetType,
            command.TargetId.Trim(),
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken);
        if (metadata is null)
        {
            return ApplicationResult<UserRatingResult>.Failure(RatingApplicationErrors.TargetNotFound());
        }

        if (!metadata.CanReceiveVisitorRatings)
        {
            return ApplicationResult<UserRatingResult>.Failure(RatingApplicationErrors.TargetUnavailable());
        }

        UserRating? retainedRating = null;
        if (metadata.TargetType == RatingTargetType.ParkItem)
        {
            retainedRating = await this.ratingRepository.GetUserRatingAsync(
                command.UserId.Trim(),
                metadata.TargetType,
                metadata.TargetId,
                cancellationToken);
        }

        RatingRankingPreparedMutation? preparedMutation =
            await RatingRankingMutationCompletion.PrepareAsync(
                command.UserId,
                metadata.TargetType,
                metadata.TargetId,
                metadata,
                retainedRating?.ParkItemCategory,
                this.parkRepository,
                this.parkItemRepository,
                this.ratingRepository,
                this.rankingMutationGuard,
                cancellationToken);
        if (preparedMutation is null)
        {
            return ApplicationResult<UserRatingResult>.Failure(
                RatingApplicationErrors.TargetChangedConcurrently());
        }

        metadata = preparedMutation.Metadata;
        if (metadata is null)
        {
            await RatingRankingMutationCompletion.AbortAsync(
                preparedMutation,
                this.ratingRepository,
                this.rankingMutationGuard);
            return ApplicationResult<UserRatingResult>.Failure(
                RatingApplicationErrors.TargetNotFound());
        }

        if (!metadata.CanReceiveVisitorRatings)
        {
            await RatingRankingMutationCompletion.AbortAsync(
                preparedMutation,
                this.ratingRepository,
                this.rankingMutationGuard);
            return ApplicationResult<UserRatingResult>.Failure(
                RatingApplicationErrors.TargetUnavailable());
        }

        DateTime nowUtc = DateTime.UtcNow;
        UserRating rating = new UserRating
        {
            UserId = command.UserId.Trim(),
            TargetType = metadata.TargetType,
            TargetId = metadata.TargetId,
            ParkId = metadata.ParkId,
            ParkItemCategory = metadata.ParkItemCategory,
            ParkItemType = metadata.ParkItemType,
            Value = command.Value,
            UpdatedAtUtc = nowUtc,
        };

        RatingAggregateTarget aggregateTarget = new RatingAggregateTarget(
            metadata.TargetType,
            metadata.TargetId,
            metadata.ParkId,
            metadata.ParkItemCategory,
            metadata.ParkItemType);
        UserRatingMutationResult mutation =
            await this.ratingRepository.UpsertUserRatingAndRecalculateAggregateAsync(
                rating,
                aggregateTarget,
                preparedMutation.RecoveryTarget.MutationToken,
                cancellationToken);
        if (mutation.WasFencedOut)
        {
            await RatingRankingMutationCompletion.AbortAsync(
                preparedMutation,
                this.ratingRepository,
                this.rankingMutationGuard);
            return ApplicationResult<UserRatingResult>.Failure(
                RatingApplicationErrors.TargetChangedConcurrently());
        }

        await this.ratingRepository.ReleaseMutationFenceAsync(
            preparedMutation.RecoveryTarget,
            CancellationToken.None);

        if (mutation.SourceChanged)
        {
            this.ratingRankProvider.Invalidate();
        }

        await RatingRankingMutationCompletion.CompleteAfterWriteAsync(
            metadata.TargetType,
            metadata.TargetId,
            preparedMutation,
            mutation.SourceChanged,
            this.parkRepository,
            this.parkItemRepository,
            this.rankingMutationGuard);

        RatingSummaryResult summary = RatingResultFactory.CreateSummary(
            metadata.TargetType,
            metadata.TargetId,
            mutation.Aggregate,
            metadata.CanReceiveVisitorRatings,
            aggregateIntegrityIsValid: mutation.Aggregate is null ? false : null);

        return ApplicationResult<UserRatingResult>.Success(ToUserRatingResult(mutation.Rating, summary));
    }

    private static UserRatingResult ToUserRatingResult(UserRating rating, RatingSummaryResult summary)
    {
        return new UserRatingResult(
            rating.Id,
            rating.UserId,
            rating.TargetType,
            rating.TargetId,
            rating.ParkId,
            rating.ParkItemCategory,
            rating.ParkItemType,
            rating.Value,
            rating.CreatedAtUtc,
            rating.UpdatedAtUtc,
            summary);
    }
}
