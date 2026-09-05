using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetSharedUserParkRatingRankingsQueryHandler
    : IQueryHandler<GetSharedUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>>
{
    private readonly UserRankingShareAccessResolver accessResolver;
    private readonly IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> rankingsHandler;

    public GetSharedUserParkRatingRankingsQueryHandler(
        UserRankingShareAccessResolver accessResolver,
        IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> rankingsHandler)
    {
        this.accessResolver = accessResolver;
        this.rankingsHandler = rankingsHandler;
    }

    public async Task<ApplicationResult<PagedResult<UserParkRatingRankingResult>>> HandleAsync(
        GetSharedUserParkRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<UserRankingShareOwner> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<PagedResult<UserParkRatingRankingResult>>.Failure(ownerResult.Errors);
        }

        return await this.rankingsHandler.HandleAsync(
            new GetUserParkRatingRankingsQuery(
                ownerResult.Value.UserId,
                query.Paging,
                query.ParkSearch,
                PublicTargetsOnly: true),
            cancellationToken);
    }
}
