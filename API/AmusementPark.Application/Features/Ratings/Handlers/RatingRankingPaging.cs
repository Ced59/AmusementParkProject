using AmusementPark.Application.Common.Results;

namespace AmusementPark.Application.Features.Ratings.Handlers;

internal static class RatingRankingPaging
{
    public static PagedResult<T> BuildPage<T>(IReadOnlyCollection<T> rankings, int page, int pageSize)
    {
        IReadOnlyCollection<T> pageItems = rankings
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>(pageItems, page, pageSize, rankings.Count);
    }
}
