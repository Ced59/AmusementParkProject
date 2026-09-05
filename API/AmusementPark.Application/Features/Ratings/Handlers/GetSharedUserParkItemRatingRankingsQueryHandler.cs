using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetSharedUserParkItemRatingRankingsQueryHandler
    : IQueryHandler<GetSharedUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>
{
    private readonly UserRankingShareAccessResolver accessResolver;
    private readonly IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> rankingsHandler;

    public GetSharedUserParkItemRatingRankingsQueryHandler(
        UserRankingShareAccessResolver accessResolver,
        IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> rankingsHandler)
    {
        this.accessResolver = accessResolver;
        this.rankingsHandler = rankingsHandler;
    }

    public async Task<ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> HandleAsync(
        GetSharedUserParkItemRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<UserRankingShareOwner> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(ownerResult.Errors);
        }

        return await this.rankingsHandler.HandleAsync(
            new GetUserParkItemRatingRankingsQuery(
                ownerResult.Value.UserId,
                query.ParkItemCategory,
                query.Paging,
                query.Search,
                query.ParkItemType,
                PublicTargetsOnly: true),
            cancellationToken);
    }
}
