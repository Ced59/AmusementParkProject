using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingRebuildScheduler
{
    Task<RatingRankingRebuildScheduleDisposition> ScheduleIfOutstandingAsync(
        RatingRankingSourceRevision sourceRevision,
        CancellationToken cancellationToken);

    Task ScheduleOutstandingAsync(CancellationToken cancellationToken);
}
