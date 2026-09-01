using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
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
        RatingRankingMutationPreparation rankingPreparation = await this.rankingMutationGuard.PrepareMutationAsync(
            metadata.TargetType,
            metadata.ParkItemCategory,
            retainedRating?.ParkItemCategory,
            cancellationToken);
        UserRatingMutationResult mutation =
            await this.ratingRepository.UpsertUserRatingAndRecalculateAggregateAsync(
                rating,
                aggregateTarget,
                cancellationToken);
        RatingRankingMutationPreparation? authoritativeCategoryPreparation =
            await RatingRankingMutationCompletion.PrepareAuthoritativeParkItemCategoryAsync(
                metadata.TargetType,
                metadata.TargetId,
                metadata.ParkItemCategory,
                retainedRating?.ParkItemCategory,
                this.parkItemRepository,
                this.rankingMutationGuard);

        this.ratingRankProvider.Invalidate();
        await this.rankingMutationGuard.CompleteMutationAsync(
            rankingPreparation,
            sourceChanged: true,
            CancellationToken.None);
        if (authoritativeCategoryPreparation is not null)
        {
            await this.rankingMutationGuard.CompleteMutationAsync(
                authoritativeCategoryPreparation,
                sourceChanged: true,
                CancellationToken.None);
        }

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

        RatingRankingMutationPreparation rankingPreparation = await this.rankingMutationGuard.PrepareMutationAsync(
            command.TargetType,
            metadata?.ParkItemCategory,
            retainedRating?.ParkItemCategory,
            cancellationToken);
        RatingAggregate? aggregate =
            await this.ratingRepository.DeleteUserRatingAndRecalculateAggregateAsync(
                userId,
                command.TargetType,
                targetId,
                cancellationToken);
        RatingRankingMutationPreparation? authoritativeCategoryPreparation =
            await RatingRankingMutationCompletion.PrepareAuthoritativeParkItemCategoryAsync(
                command.TargetType,
                targetId,
                metadata?.ParkItemCategory,
                retainedRating?.ParkItemCategory,
                this.parkItemRepository,
                this.rankingMutationGuard);

        this.ratingRankProvider.Invalidate();
        await this.rankingMutationGuard.CompleteMutationAsync(
            rankingPreparation,
            sourceChanged: true,
            CancellationToken.None);
        if (authoritativeCategoryPreparation is not null)
        {
            await this.rankingMutationGuard.CompleteMutationAsync(
                authoritativeCategoryPreparation,
                sourceChanged: true,
                CancellationToken.None);
        }

        RatingSummaryResult summary = RatingResultFactory.CreateSummary(
            command.TargetType,
            targetId,
            aggregate,
            metadata?.CanReceiveVisitorRatings ?? false,
            aggregateIntegrityIsValid: aggregate is null ? true : null);
        return ApplicationResult<RatingSummaryResult>.Success(summary);
    }
}

internal static class RatingRankingMutationCompletion
{
    public static async Task<RatingRankingMutationPreparation?> PrepareAuthoritativeParkItemCategoryAsync(
        RatingTargetType targetType,
        string targetId,
        ParkItemCategory? observedCategory,
        ParkItemCategory? retainedCategory,
        IParkItemRepository parkItemRepository,
        IRatingRankingMutationGuard rankingMutationGuard)
    {
        if (targetType != RatingTargetType.ParkItem)
        {
            return null;
        }

        ParkItem? currentParkItem = await parkItemRepository.GetByIdAsync(
            targetId,
            includeHidden: false,
            cancellationToken: CancellationToken.None);
        ParkItemCategory? authoritativeCategory = currentParkItem?.Category;
        if (!authoritativeCategory.HasValue
            || authoritativeCategory == observedCategory
            || authoritativeCategory == retainedCategory)
        {
            return null;
        }

        return await rankingMutationGuard.PrepareMutationAsync(
            RatingTargetType.ParkItem,
            authoritativeCategory,
            previousParkItemCategory: null,
            cancellationToken: CancellationToken.None);
    }
}

public sealed class GetRatingSummaryQueryHandler : IQueryHandler<GetRatingSummaryQuery, ApplicationResult<RatingSummaryResult>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly IRatingRankProvider ratingRankProvider;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public GetRatingSummaryQueryHandler(
        IRatingRepository ratingRepository,
        IRatingRankProvider ratingRankProvider,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.ratingRepository = ratingRepository;
        this.ratingRankProvider = ratingRankProvider;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<RatingSummaryResult>> HandleAsync(GetRatingSummaryQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.TargetId))
        {
            return ApplicationResult<RatingSummaryResult>.Failure(ApplicationErrors.Required(nameof(query.TargetId)));
        }

        if (!Enum.IsDefined(query.TargetType))
        {
            return ApplicationResult<RatingSummaryResult>.Failure(RatingApplicationErrors.InvalidTargetType());
        }

        string targetId = query.TargetId.Trim();
        RatingTargetMetadataResult? metadata = await RatingTargetMetadataResolver.ResolveAsync(
            query.TargetType,
            targetId,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken);
        if (metadata is null)
        {
            return ApplicationResult<RatingSummaryResult>.Failure(RatingApplicationErrors.TargetNotFound());
        }

        if (!metadata.CanReceiveVisitorRatings)
        {
            return ApplicationResult<RatingSummaryResult>.Failure(RatingApplicationErrors.TargetUnavailable());
        }

        RatingAggregate? aggregate = await this.ratingRepository.GetAggregateAsync(query.TargetType, targetId, cancellationToken);
        RatingSummaryResult summary = RatingResultFactory.CreateSummary(
            query.TargetType,
            targetId,
            aggregate,
            metadata.CanReceiveVisitorRatings,
            aggregateIntegrityIsValid: aggregate is null ? true : null);
        if (aggregate is not null && aggregate.RatingCount > 0)
        {
            int? rank = await this.ratingRankProvider.GetRankAsync(aggregate, cancellationToken);
            summary = summary with { Rank = rank };
        }

        return ApplicationResult<RatingSummaryResult>.Success(summary);
    }
}

internal static class RatingTargetMetadataResolver
{
    public static async Task<RatingTargetMetadataResult?> ResolveAsync(
        RatingTargetType targetType,
        string targetId,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        CancellationToken cancellationToken)
    {
        if (targetType == RatingTargetType.Park)
        {
            Park? park = await parkRepository.GetByIdAsync(targetId, false, cancellationToken);
            if (park is null || string.IsNullOrWhiteSpace(park.Id))
            {
                return null;
            }

            return new RatingTargetMetadataResult(
                RatingTargetType.Park,
                park.Id.Trim(),
                park.Name?.Trim() ?? park.Id.Trim(),
                park.Id.Trim(),
                park.Name?.Trim(),
                null,
                null,
                park.Status.CanReceiveVisitorRatings());
        }

        if (targetType == RatingTargetType.ParkItem)
        {
            ParkItem? item = await parkItemRepository.GetByIdAsync(targetId, false, cancellationToken);
            if (item is null || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.ParkId))
            {
                return null;
            }

            Park? park = await parkRepository.GetByIdAsync(item.ParkId, false, cancellationToken);
            if (park is null || string.IsNullOrWhiteSpace(park.Id))
            {
                return null;
            }

            bool canReceiveVisitorRatings = park.Status.CanReceiveVisitorRatings()
                && ParkItemStatusNormalizer.CanReceiveVisitorRatings(item.Category, item.AttractionDetails?.Status);

            return new RatingTargetMetadataResult(
                RatingTargetType.ParkItem,
                item.Id.Trim(),
                item.Name.Trim(),
                park.Id.Trim(),
                park.Name?.Trim(),
                item.Category,
                item.Type,
                canReceiveVisitorRatings);
        }

        return null;
    }
}

public sealed class GetUserRatingQueryHandler : IQueryHandler<GetUserRatingQuery, ApplicationResult<UserRatingResult?>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public GetUserRatingQueryHandler(
        IRatingRepository ratingRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.ratingRepository = ratingRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<UserRatingResult?>> HandleAsync(GetUserRatingQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<UserRatingResult?>.Failure(ApplicationErrors.Required(nameof(query.UserId)));
        }

        if (string.IsNullOrWhiteSpace(query.TargetId))
        {
            return ApplicationResult<UserRatingResult?>.Failure(ApplicationErrors.Required(nameof(query.TargetId)));
        }

        if (!Enum.IsDefined(query.TargetType))
        {
            return ApplicationResult<UserRatingResult?>.Failure(RatingApplicationErrors.InvalidTargetType());
        }

        UserRating? rating = await this.ratingRepository.GetUserRatingAsync(query.UserId.Trim(), query.TargetType, query.TargetId.Trim(), cancellationToken);
        if (rating is null)
        {
            return ApplicationResult<UserRatingResult?>.Success(null);
        }

        string targetId = query.TargetId.Trim();
        RatingTargetMetadataResult? metadata = await RatingTargetMetadataResolver.ResolveAsync(
            query.TargetType,
            targetId,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken);
        RatingAggregate? aggregate = await this.ratingRepository.GetAggregateAsync(
            query.TargetType,
            targetId,
            cancellationToken);
        RatingSummaryResult summary = RatingResultFactory.CreateSummary(
            query.TargetType,
            targetId,
            aggregate,
            metadata?.CanReceiveVisitorRatings ?? false,
            aggregateIntegrityIsValid: aggregate is null ? false : null);

        UserRatingResult result = new UserRatingResult(
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

        return ApplicationResult<UserRatingResult?>.Success(result);
    }
}

public sealed class ListUserRatingsQueryHandler : IQueryHandler<ListUserRatingsQuery, ApplicationResult<PagedResult<UserRatingListItemResult>>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public ListUserRatingsQueryHandler(IRatingRepository ratingRepository, PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<UserRatingListItemResult>>> HandleAsync(ListUserRatingsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<PagedResult<UserRatingListItemResult>>.Failure(ApplicationErrors.Required(nameof(query.UserId)));
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<UserRatingListItemResult>>.Failure(errors);
        }

        PagedResult<UserRatingListItemResult> result = await this.ratingRepository.GetUserRatingsAsync(
            query.UserId.Trim(),
            query.Paging.Page,
            query.Paging.PageSize,
            query.ParkSearch,
            cancellationToken);

        return ApplicationResult<PagedResult<UserRatingListItemResult>>.Success(result);
    }
}

public sealed class GetUserRatingStatsQueryHandler : IQueryHandler<GetUserRatingStatsQuery, ApplicationResult<UserRatingStatsResult>>
{
    private readonly IRatingRepository ratingRepository;

    public GetUserRatingStatsQueryHandler(IRatingRepository ratingRepository)
    {
        this.ratingRepository = ratingRepository;
    }

    public async Task<ApplicationResult<UserRatingStatsResult>> HandleAsync(GetUserRatingStatsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<UserRatingStatsResult>.Failure(ApplicationErrors.Required(nameof(query.UserId)));
        }

        UserRatingStatsResult result = await this.ratingRepository.GetUserRatingStatsAsync(query.UserId.Trim(), cancellationToken);
        return ApplicationResult<UserRatingStatsResult>.Success(result);
    }
}
