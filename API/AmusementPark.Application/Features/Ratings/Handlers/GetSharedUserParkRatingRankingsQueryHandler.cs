using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetSharedUserParkRatingRankingsQueryHandler
    : IQueryHandler<GetSharedUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>>
{
    private readonly ISharePublicationAccessResolver accessResolver;
    private readonly IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> rankingsHandler;

    public GetSharedUserParkRatingRankingsQueryHandler(
        ISharePublicationAccessResolver accessResolver,
        IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> rankingsHandler)
    {
        this.accessResolver = accessResolver;
        this.rankingsHandler = rankingsHandler;
    }

    public async Task<ApplicationResult<PagedResult<UserParkRatingRankingResult>>> HandleAsync(
        GetSharedUserParkRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ResolvedSharePublicationResult> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            SharePublicationType.PersonalRanking,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<PagedResult<UserParkRatingRankingResult>>.Failure(ownerResult.Errors);
        }

        return await this.rankingsHandler.HandleAsync(
            new GetUserParkRatingRankingsQuery(
                ownerResult.Value.OwnerUserId,
                query.Paging,
                query.ParkSearch,
                PublicTargetsOnly: true),
            cancellationToken);
    }
}
