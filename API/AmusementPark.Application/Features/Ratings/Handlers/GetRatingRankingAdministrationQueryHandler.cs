using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetRatingRankingAdministrationQueryHandler
    : IQueryHandler<GetRatingRankingAdministrationQuery, ApplicationResult<RatingRankingAdministrationResult>>
{
    private readonly RatingRankingAdministrationDashboardReader dashboardReader;

    public GetRatingRankingAdministrationQueryHandler(
        RatingRankingAdministrationDashboardReader dashboardReader)
    {
        this.dashboardReader = dashboardReader;
    }

    public async Task<ApplicationResult<RatingRankingAdministrationResult>> HandleAsync(
        GetRatingRankingAdministrationQuery query,
        CancellationToken cancellationToken = default)
    {
        RatingRankingAdministrationResult result =
            await this.dashboardReader.GetDashboardAsync(cancellationToken);
        return ApplicationResult<RatingRankingAdministrationResult>.Success(result);
    }
}
