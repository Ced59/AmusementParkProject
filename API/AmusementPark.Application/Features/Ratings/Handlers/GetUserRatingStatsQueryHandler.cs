using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Ratings.Handlers;

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
