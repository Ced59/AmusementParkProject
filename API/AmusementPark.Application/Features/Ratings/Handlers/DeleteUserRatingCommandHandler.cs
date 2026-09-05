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

public sealed class DeleteUserRatingCommandHandler : ICommandHandler<DeleteUserRatingCommand, ApplicationResult<RatingSummaryResult>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly IRatingRankProvider ratingRankProvider;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IRatingRankingMutationGuard rankingMutationGuard;

    public DeleteUserRatingCommandHandler(
        IRatingRepository ratingRepository,
        IRatingRankProvider ratingRankProvider,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IRatingRankingMutationGuard rankingMutationGuard)
    {
        this.ratingRepository = ratingRepository;
        this.ratingRankProvider = ratingRankProvider;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.rankingMutationGuard = rankingMutationGuard;
    }

    public async Task<ApplicationResult<RatingSummaryResult>> HandleAsync(
        DeleteUserRatingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<RatingSummaryResult>.Failure(ApplicationErrors.Required(nameof(command.UserId)));
        }

        if (string.IsNullOrWhiteSpace(command.TargetId))
        {
            return ApplicationResult<RatingSummaryResult>.Failure(ApplicationErrors.Required(nameof(command.TargetId)));
        }

        if (!Enum.IsDefined(command.TargetType))
        {
            return ApplicationResult<RatingSummaryResult>.Failure(RatingApplicationErrors.InvalidTargetType());
        }

        string userId = command.UserId.Trim();
        string targetId = command.TargetId.Trim();
        RatingTargetMetadataResult? metadata = await RatingTargetMetadataResolver.ResolveAsync(
            command.TargetType,
            targetId,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken);
        UserRating? retainedRating = null;
        if (command.TargetType == RatingTargetType.ParkItem)
        {
            retainedRating = await this.ratingRepository.GetUserRatingAsync(
                userId,
                command.TargetType,
                targetId,
                cancellationToken);
        }

        RatingRankingPreparedMutation? preparedMutation =
            await RatingRankingMutationCompletion.PrepareAsync(
                userId,
                command.TargetType,
                targetId,
                metadata,
                retainedRating?.ParkItemCategory,
                this.parkRepository,
                this.parkItemRepository,
                this.ratingRepository,
                this.rankingMutationGuard,
                cancellationToken);
        if (preparedMutation is null)
        {
            return ApplicationResult<RatingSummaryResult>.Failure(
                RatingApplicationErrors.TargetChangedConcurrently());
        }

        metadata = preparedMutation.Metadata;
        UserRatingDeletionResult mutation =
            await this.ratingRepository.DeleteUserRatingAndRecalculateAggregateAsync(
                userId,
                command.TargetType,
                targetId,
                preparedMutation.RecoveryTarget.MutationToken,
                cancellationToken);
        if (mutation.WasFencedOut)
        {
            await RatingRankingMutationCompletion.AbortAsync(
                preparedMutation,
                this.ratingRepository,
                this.rankingMutationGuard);
            return ApplicationResult<RatingSummaryResult>.Failure(
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
            command.TargetType,
            targetId,
            preparedMutation,
            mutation.SourceChanged,
            this.parkRepository,
            this.parkItemRepository,
            this.rankingMutationGuard);

        RatingSummaryResult summary = RatingResultFactory.CreateSummary(
            command.TargetType,
            targetId,
            mutation.Aggregate,
            metadata?.CanReceiveVisitorRatings ?? false,
            aggregateIntegrityIsValid: mutation.Aggregate is null ? true : null);
        return ApplicationResult<RatingSummaryResult>.Success(summary);
    }
}
