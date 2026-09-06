using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetSharedUserRankingProfileQueryHandler
    : IQueryHandler<GetSharedUserRankingProfileQuery, ApplicationResult<SharedUserRankingProfileResult>>
{
    private readonly ISharePublicationAccessResolver accessResolver;
    private readonly IRatingRepository ratingRepository;

    public GetSharedUserRankingProfileQueryHandler(
        ISharePublicationAccessResolver accessResolver,
        IRatingRepository ratingRepository)
    {
        this.accessResolver = accessResolver;
        this.ratingRepository = ratingRepository;
    }

    public async Task<ApplicationResult<SharedUserRankingProfileResult>> HandleAsync(
        GetSharedUserRankingProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ResolvedSharePublicationResult> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            SharePublicationType.PersonalRanking,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<SharedUserRankingProfileResult>.Failure(ownerResult.Errors);
        }

        UserRatingStatsResult stats = await this.ratingRepository.GetVisibleUserRatingStatsAsync(
            ownerResult.Value.OwnerUserId,
            cancellationToken);
        return ApplicationResult<SharedUserRankingProfileResult>.Success(
            new SharedUserRankingProfileResult(
                ownerResult.Value.OwnerUserId,
                ownerResult.Value.DisplayName,
                ownerResult.Value.PublishedAtUtc,
                stats));
    }
}
