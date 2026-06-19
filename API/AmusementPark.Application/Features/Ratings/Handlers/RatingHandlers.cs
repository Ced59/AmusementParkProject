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

public sealed class GetRatingRankingsQueryHandler : IQueryHandler<GetRatingRankingsQuery, ApplicationResult<PagedResult<ParkRatingRankingResult>>>
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRepository ratingRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetRatingRankingsQueryHandler(IRatingRepository ratingRepository, PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<ParkRatingRankingResult>>> HandleAsync(GetRatingRankingsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkRatingRankingResult>>.Failure(errors);
        }

        IReadOnlyCollection<RatingRankingItemResult> sources = await this.ratingRepository.GetVisibleRankingSourcesAsync(
            query.ParkItemCategory,
            RankingSourceLimit,
            cancellationToken);

        IReadOnlyCollection<ParkRatingRankingResult> rankings = BuildParkRankings(sources, query.ParkItemCategory);
        PagedResult<ParkRatingRankingResult> result = string.IsNullOrWhiteSpace(query.ParkSearch)
            ? BuildPagedRankings(rankings, query.Paging.Page, query.Paging.PageSize)
            : BuildSearchWindow(rankings, query.ParkSearch.Trim(), query.Paging.PageSize);

        return ApplicationResult<PagedResult<ParkRatingRankingResult>>.Success(result);
    }

    private static PagedResult<ParkRatingRankingResult> BuildPagedRankings(IReadOnlyCollection<ParkRatingRankingResult> rankings, int page, int pageSize)
    {
        IReadOnlyCollection<ParkRatingRankingResult> pageItems = rankings
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ParkRatingRankingResult>(pageItems, page, pageSize, rankings.Count);
    }

    private static PagedResult<ParkRatingRankingResult> BuildSearchWindow(IReadOnlyCollection<ParkRatingRankingResult> rankings, string parkSearch, int requestedPageSize)
    {
        List<ParkRatingRankingResult> orderedRankings = rankings.ToList();
        int matchIndex = orderedRankings.FindIndex(ranking => ranking.ParkName.Contains(parkSearch, StringComparison.OrdinalIgnoreCase));
        if (matchIndex < 0)
        {
            return new PagedResult<ParkRatingRankingResult>(Array.Empty<ParkRatingRankingResult>(), 1, requestedPageSize, 0);
        }

        const int contextSize = 5;
        int startIndex = Math.Max(0, matchIndex - contextSize);
        int endIndex = Math.Min(orderedRankings.Count - 1, matchIndex + contextSize);
        List<ParkRatingRankingResult> items = orderedRankings
            .Skip(startIndex)
            .Take(endIndex - startIndex + 1)
            .ToList();

        return new PagedResult<ParkRatingRankingResult>(items, 1, Math.Max(items.Count, 1), items.Count);
    }

    private static IReadOnlyCollection<ParkRatingRankingResult> BuildParkRankings(IReadOnlyCollection<RatingRankingItemResult> sources, ParkItemCategory? categoryFilter)
    {
        List<ParkRatingRankingResult> rankings = sources
            .Where(static source => !string.IsNullOrWhiteSpace(source.ParkId))
            .GroupBy(static source => source.ParkId, StringComparer.Ordinal)
            .Select(group => BuildParkRanking(group.Key, group.ToList(), categoryFilter))
            .Where(static ranking => ranking is not null)
            .Select(static ranking => ranking!)
            .OrderByDescending(static ranking => ranking.Score)
            .ThenByDescending(static ranking => ranking.RatingCount)
            .ThenBy(static ranking => ranking.ParkName, StringComparer.OrdinalIgnoreCase)
            .Select(static (ranking, index) => ranking with { Rank = index + 1 })
            .ToList();

        return rankings;
    }

    private static ParkRatingRankingResult? BuildParkRanking(string parkId, IReadOnlyCollection<RatingRankingItemResult> sources, ParkItemCategory? categoryFilter)
    {
        RatingRankingItemResult? directParkSource = sources.FirstOrDefault(static source => source.TargetType == RatingTargetType.Park);
        List<RatingRankingItemResult> itemSources = sources
            .Where(static source => source.TargetType == RatingTargetType.ParkItem && source.ParkItemCategory.HasValue)
            .ToList();

        if (categoryFilter.HasValue && itemSources.Count == 0)
        {
            return null;
        }

        List<ParkRatingRankingCategoryResult> categories = itemSources
            .GroupBy(static source => source.ParkItemCategory!.Value)
            .Select(static group => BuildCategoryRanking(group.Key, group.ToList()))
            .OrderByDescending(static category => category.BayesianScore)
            .ThenBy(static category => category.ParkItemCategory)
            .ToList();

        double? directParkScore = directParkSource?.BayesianScore;
        double? itemsScore = categories.Count == 0
            ? null
            : RatingScoreCalculator.CalculateCategoryBalancedItemsScore(categories.Select(static category => category.BayesianScore).ToList());
        double score = RatingScoreCalculator.CalculateCompositeParkScore(directParkScore, itemsScore);
        long parkRatingCount = directParkSource?.RatingCount ?? 0;
        long itemRatingCount = itemSources.Sum(static source => source.RatingCount);
        long totalRatingCount = parkRatingCount + itemRatingCount;
        double itemRatingSum = itemSources.Sum(static source => source.RatingSum);
        string parkName = directParkSource?.TargetName
            ?? sources.Select(static source => source.ParkName).FirstOrDefault(static name => !string.IsNullOrWhiteSpace(name))?.Trim()
            ?? parkId;

        return new ParkRatingRankingResult(
            0,
            parkId,
            parkName,
            totalRatingCount,
            score,
            parkRatingCount,
            directParkSource?.AverageRating ?? 0d,
            itemRatingCount,
            RatingScoreCalculator.CalculateAverage(itemRatingSum, itemRatingCount),
            categories);
    }

    private static ParkRatingRankingCategoryResult BuildCategoryRanking(ParkItemCategory category, IReadOnlyCollection<RatingRankingItemResult> sources)
    {
        long ratingCount = sources.Sum(static source => source.RatingCount);
        double ratingSum = sources.Sum(static source => source.RatingSum);
        List<ParkRatingRankingItemResult> items = sources
            .OrderByDescending(static source => source.BayesianScore)
            .ThenByDescending(static source => source.RatingCount)
            .ThenBy(static source => source.TargetName, StringComparer.OrdinalIgnoreCase)
            .Select(static source => new ParkRatingRankingItemResult(
                source.TargetId,
                source.TargetName,
                source.ParkItemCategory,
                source.ParkItemType,
                source.RatingCount,
                source.AverageRating,
                source.BayesianScore))
            .ToList();

        return new ParkRatingRankingCategoryResult(
            category,
            ratingCount,
            RatingScoreCalculator.CalculateAverage(ratingSum, ratingCount),
            RatingScoreCalculator.CalculateBayesianScore(ratingSum, ratingCount),
            items);
    }
}
