using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetSharedUserParkItemRatingRankingsQueryHandler
    : IQueryHandler<GetSharedUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>
{
    private readonly ISharePublicationAccessResolver accessResolver;
    private readonly IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> rankingsHandler;

    public GetSharedUserParkItemRatingRankingsQueryHandler(
        ISharePublicationAccessResolver accessResolver,
        IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> rankingsHandler)
    {
        this.accessResolver = accessResolver;
        this.rankingsHandler = rankingsHandler;
    }

    public async Task<ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> HandleAsync(
        GetSharedUserParkItemRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ResolvedSharePublicationResult> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            SharePublicationType.PersonalRanking,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(ownerResult.Errors);
        }

        ApplicationResult<PagedResult<UserParkItemRatingRankingResult>> rankingsResult =
            await this.rankingsHandler.HandleAsync(
                new GetUserParkItemRatingRankingsQuery(
                    ownerResult.Value.OwnerUserId,
                    query.ParkItemCategory,
                    query.Paging,
                    query.Search,
                    query.ParkItemType,
                    PublicTargetsOnly: true),
                cancellationToken);
        if (!rankingsResult.IsSuccess)
        {
            return rankingsResult;
        }

        ApplicationResult<bool> revalidation = await this.accessResolver.RevalidateAsync(
            query.ShareId,
            ownerResult.Value,
            cancellationToken);
        return revalidation.IsSuccess
            ? rankingsResult
            : ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(
                revalidation.Errors);
    }
}
