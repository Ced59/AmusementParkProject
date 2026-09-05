namespace AmusementPark.Application.Features.Ratings.Models;

public static class RatingRankingSnapshotBuildLimits
{
    public const int ParkCandidateBatchSize = 50;
    public const int MaximumSourceComponentCountPerParkBatch = 50000;
}
