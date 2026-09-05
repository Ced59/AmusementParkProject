using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetUserParkRatingRankingsQueryHandler
    : IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>>
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRepository ratingRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetUserParkRatingRankingsQueryHandler(
        IRatingRepository ratingRepository,
        PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<UserParkRatingRankingResult>>> HandleAsync(
        GetUserParkRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<PagedResult<UserParkRatingRankingResult>>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<UserParkRatingRankingResult>>.Failure(errors);
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
        IReadOnlyCollection<UserParkRatingRankingResult> rankings = RatingRankingFactory.BuildUserParkRankings(sources);
        PagedResult<UserParkRatingRankingResult> result;
        if (!string.IsNullOrWhiteSpace(query.TargetId))
        {
            IReadOnlyCollection<UserParkRatingRankingResult> exactTarget = rankings
                .Where(ranking => string.Equals(
                    ranking.ParkId,
                    query.TargetId.Trim(),
                    StringComparison.Ordinal))
                .ToArray();
            result = RatingRankingPaging.BuildPage(exactTarget, 1, query.Paging.PageSize);
        }
        else
        {
            result = string.IsNullOrWhiteSpace(query.ParkSearch)
                ? RatingRankingPaging.BuildPage(rankings, query.Paging.Page, query.Paging.PageSize)
                : BuildSearchWindow(rankings, query.ParkSearch.Trim(), query.Paging.PageSize);
        }

        return ApplicationResult<PagedResult<UserParkRatingRankingResult>>.Success(result);
    }

    private static PagedResult<UserParkRatingRankingResult> BuildSearchWindow(
        IReadOnlyCollection<UserParkRatingRankingResult> rankings,
        string parkSearch,
        int requestedPageSize)
    {
        List<UserParkRatingRankingResult> orderedRankings = rankings.ToList();
        int matchIndex = orderedRankings.FindIndex(ranking =>
            ranking.ParkName.Contains(parkSearch, StringComparison.OrdinalIgnoreCase));
        if (matchIndex < 0)
        {
            return new PagedResult<UserParkRatingRankingResult>(
                Array.Empty<UserParkRatingRankingResult>(),
                1,
                requestedPageSize,
                0);
        }

        const int contextSize = 5;
        int startIndex = Math.Max(0, matchIndex - contextSize);
        int endIndex = Math.Min(orderedRankings.Count - 1, matchIndex + contextSize);
        List<UserParkRatingRankingResult> items = orderedRankings
            .Skip(startIndex)
            .Take(endIndex - startIndex + 1)
            .ToList();

        return new PagedResult<UserParkRatingRankingResult>(
            items,
            1,
            Math.Max(items.Count, 1),
            items.Count);
    }
}
