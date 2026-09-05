using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class RebuildRatingRankingSnapshotsCommandHandler
    : ICommandHandler<RebuildRatingRankingSnapshotsCommand, ApplicationResult<RatingRankingRebuildRequestResult>>
{
    private readonly RatingRankingRebuildRequester rebuildRequester;

    public RebuildRatingRankingSnapshotsCommandHandler(
        RatingRankingRebuildRequester rebuildRequester)
    {
        this.rebuildRequester = rebuildRequester;
    }

    public async Task<ApplicationResult<RatingRankingRebuildRequestResult>> HandleAsync(
        RebuildRatingRankingSnapshotsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Confirmed)
        {
            return ApplicationResult<RatingRankingRebuildRequestResult>.Failure(
                RatingApplicationErrors.RankingRebuildConfirmationRequired());
        }

        RatingRankingRebuildRequestResult result =
            await this.rebuildRequester.RequestRebuildAsync(cancellationToken);
        return ApplicationResult<RatingRankingRebuildRequestResult>.Success(result);
    }
}
