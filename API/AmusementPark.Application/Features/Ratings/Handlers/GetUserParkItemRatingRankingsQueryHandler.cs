using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetUserParkItemRatingRankingsQueryHandler
    : IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRepository ratingRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetUserParkItemRatingRankingsQueryHandler(
        IRatingRepository ratingRepository,
        PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> HandleAsync(
        GetUserParkItemRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(errors);
        }

        if (!Enum.IsDefined(query.ParkItemCategory))
        {
            return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(
                RatingApplicationErrors.InvalidParkItemCategory());
        }

        IReadOnlyCollection<UserRatingListItemResult> sources = query.PublicTargetsOnly
            ? await this.ratingRepository.GetVisibleUserRankingSourcesAsync(
                query.UserId.Trim(),
                RankingSourceLimit,
                cancellationToken)
            : await this.ratingRepository.GetUserRankingSourcesAsync(
                query.UserId.Trim(),
                RankingSourceLimit,
                cancellationToken);
        string? exactTargetId = string.IsNullOrWhiteSpace(query.TargetId)
            ? null
            : query.TargetId.Trim();
        IReadOnlyCollection<UserRatingListItemResult> rankingSources = exactTargetId is null
            ? sources
            : sources.Select(source => source.TargetType == RatingTargetType.ParkItem
                    && string.Equals(source.TargetId, exactTargetId, StringComparison.Ordinal)
                ? source with { ParkItemCategory = query.ParkItemCategory }
                : source)
                .ToArray();
        IReadOnlyCollection<UserParkItemRatingRankingResult> rankings =
            RatingRankingFactory.BuildUserParkItemRankings(
                rankingSources,
                query.ParkItemCategory,
                query.ParkItemType);
        IReadOnlyCollection<UserParkItemRatingRankingResult> filteredRankings;
        if (exactTargetId is not null)
        {
            filteredRankings = rankings
                .Where(ranking => string.Equals(
                    ranking.Rating.TargetId,
                    exactTargetId,
                    StringComparison.Ordinal))
                .ToArray();
        }
        else
        {
            filteredRankings = string.IsNullOrWhiteSpace(query.Search)
                ? rankings
                : rankings.Where(ranking =>
                        ranking.Rating.TargetName.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase)
                        || (ranking.Rating.ParkName?.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
        }
        PagedResult<UserParkItemRatingRankingResult> result = RatingRankingPaging.BuildPage(
            filteredRankings,
            exactTargetId is null ? query.Paging.Page : 1,
            query.Paging.PageSize);

        return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Success(result);
    }
}
