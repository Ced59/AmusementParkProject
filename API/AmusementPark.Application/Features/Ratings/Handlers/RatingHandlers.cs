using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class UpsertUserRatingCommandHandler : ICommandHandler<UpsertUserRatingCommand, ApplicationResult<UserRatingResult>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public UpsertUserRatingCommandHandler(
        IRatingRepository ratingRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.ratingRepository = ratingRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
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

        RatingTargetMetadataResult? metadata = await this.ResolveTargetMetadataAsync(command.TargetType, command.TargetId.Trim(), cancellationToken);
        if (metadata is null)
        {
            return ApplicationResult<UserRatingResult>.Failure(RatingApplicationErrors.TargetNotFound());
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

        UserRating upsertedRating = await this.ratingRepository.UpsertUserRatingAsync(rating, cancellationToken);
        RatingAggregate? aggregate = await this.ratingRepository.RecalculateAggregateAsync(metadata, cancellationToken);
        RatingSummaryResult summary = ToSummary(metadata.TargetType, metadata.TargetId, aggregate);

        return ApplicationResult<UserRatingResult>.Success(ToUserRatingResult(upsertedRating, summary));
    }

    private async Task<RatingTargetMetadataResult?> ResolveTargetMetadataAsync(RatingTargetType targetType, string targetId, CancellationToken cancellationToken)
    {
        if (targetType == RatingTargetType.Park)
        {
            Park? park = await this.parkRepository.GetByIdAsync(targetId, false, cancellationToken);
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
                null);
        }

        if (targetType == RatingTargetType.ParkItem)
        {
            ParkItem? item = await this.parkItemRepository.GetByIdAsync(targetId, false, cancellationToken);
            if (item is null || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.ParkId))
            {
                return null;
            }

            Park? park = await this.parkRepository.GetByIdAsync(item.ParkId, false, cancellationToken);
            if (park is null || string.IsNullOrWhiteSpace(park.Id))
            {
                return null;
            }

            return new RatingTargetMetadataResult(
                RatingTargetType.ParkItem,
                item.Id.Trim(),
                item.Name.Trim(),
                park.Id.Trim(),
                park.Name?.Trim(),
                item.Category,
                item.Type);
        }

        return null;
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

    private static RatingSummaryResult ToSummary(RatingTargetType targetType, string targetId, RatingAggregate? aggregate)
    {
        if (aggregate is null)
        {
            return new RatingSummaryResult(targetType, targetId, 0, 0d, RatingScoreCalculator.PriorMean);
        }

        return new RatingSummaryResult(
            aggregate.TargetType,
            aggregate.TargetId,
            aggregate.RatingCount,
            aggregate.AverageRating,
            aggregate.BayesianScore);
    }
}

public sealed class GetRatingSummaryQueryHandler : IQueryHandler<GetRatingSummaryQuery, ApplicationResult<RatingSummaryResult>>
{
    private readonly IRatingRepository ratingRepository;

    public GetRatingSummaryQueryHandler(IRatingRepository ratingRepository)
    {
        this.ratingRepository = ratingRepository;
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

        RatingAggregate? aggregate = await this.ratingRepository.GetAggregateAsync(query.TargetType, query.TargetId.Trim(), cancellationToken);
        RatingSummaryResult summary = aggregate is null
            ? new RatingSummaryResult(query.TargetType, query.TargetId.Trim(), 0, 0d, RatingScoreCalculator.PriorMean)
            : new RatingSummaryResult(aggregate.TargetType, aggregate.TargetId, aggregate.RatingCount, aggregate.AverageRating, aggregate.BayesianScore);

        return ApplicationResult<RatingSummaryResult>.Success(summary);
    }
}

public sealed class GetUserRatingQueryHandler : IQueryHandler<GetUserRatingQuery, ApplicationResult<UserRatingResult?>>
{
    private readonly IRatingRepository ratingRepository;

    public GetUserRatingQueryHandler(IRatingRepository ratingRepository)
    {
        this.ratingRepository = ratingRepository;
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

        RatingAggregate? aggregate = await this.ratingRepository.GetAggregateAsync(query.TargetType, query.TargetId.Trim(), cancellationToken);
        RatingSummaryResult summary = aggregate is null
            ? new RatingSummaryResult(query.TargetType, query.TargetId.Trim(), 0, 0d, RatingScoreCalculator.PriorMean)
            : new RatingSummaryResult(aggregate.TargetType, aggregate.TargetId, aggregate.RatingCount, aggregate.AverageRating, aggregate.BayesianScore);

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

public sealed class GetRatingRankingsQueryHandler : IQueryHandler<GetRatingRankingsQuery, ApplicationResult<PagedResult<RatingRankingItemResult>>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetRatingRankingsQueryHandler(IRatingRepository ratingRepository, PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<RatingRankingItemResult>>> HandleAsync(GetRatingRankingsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.TargetType.HasValue && !Enum.IsDefined(query.TargetType.Value))
        {
            return ApplicationResult<PagedResult<RatingRankingItemResult>>.Failure(RatingApplicationErrors.InvalidTargetType());
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<RatingRankingItemResult>>.Failure(errors);
        }

        PagedResult<RatingRankingItemResult> result = await this.ratingRepository.GetRankingsAsync(
            query.TargetType,
            query.ParkItemCategory,
            query.Paging.Page,
            query.Paging.PageSize,
            cancellationToken);

        return ApplicationResult<PagedResult<RatingRankingItemResult>>.Success(result);
    }
}
