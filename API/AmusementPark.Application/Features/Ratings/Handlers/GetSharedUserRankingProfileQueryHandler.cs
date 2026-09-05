using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetSharedUserRankingProfileQueryHandler
    : IQueryHandler<GetSharedUserRankingProfileQuery, ApplicationResult<SharedUserRankingProfileResult>>
{
    private readonly UserRankingShareAccessResolver accessResolver;
    private readonly IRatingRepository ratingRepository;

    public GetSharedUserRankingProfileQueryHandler(
        UserRankingShareAccessResolver accessResolver,
        IRatingRepository ratingRepository)
    {
        this.accessResolver = accessResolver;
        this.ratingRepository = ratingRepository;
    }

    public async Task<ApplicationResult<SharedUserRankingProfileResult>> HandleAsync(
        GetSharedUserRankingProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<UserRankingShareOwner> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<SharedUserRankingProfileResult>.Failure(ownerResult.Errors);
        }

        UserRatingStatsResult stats = await this.ratingRepository.GetVisibleUserRatingStatsAsync(
            ownerResult.Value.UserId,
            cancellationToken);
        return ApplicationResult<SharedUserRankingProfileResult>.Success(
            new SharedUserRankingProfileResult(
                ownerResult.Value.UserId,
                ownerResult.Value.DisplayName,
                ownerResult.Value.PublishedAtUtc,
                stats));
    }
}
