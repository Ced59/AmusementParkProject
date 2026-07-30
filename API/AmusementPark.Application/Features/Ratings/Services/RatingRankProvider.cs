using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankProvider : IRatingRankProvider
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRepository ratingRepository;
    private readonly IRatingRankSnapshotCache snapshotCache;

    public RatingRankProvider(
        IRatingRepository ratingRepository,
        IRatingRankSnapshotCache snapshotCache)
    {
        this.ratingRepository = ratingRepository;
        this.snapshotCache = snapshotCache;
    }

    public async Task<int?> GetRankAsync(RatingAggregate aggregate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        if (aggregate.TargetType == RatingTargetType.Park)
        {
            IReadOnlyDictionary<string, int> ranks = await this.snapshotCache.GetOrCreateAsync(
                RatingTargetType.Park,
                null,
                this.BuildParkRanksAsync,
                cancellationToken);
            return ranks.TryGetValue(aggregate.TargetId, out int rank) ? rank : null;
        }

        if (aggregate.TargetType == RatingTargetType.ParkItem && aggregate.ParkItemCategory.HasValue)
        {
            ParkItemCategory category = aggregate.ParkItemCategory.Value;
            IReadOnlyDictionary<string, int> ranks = await this.snapshotCache.GetOrCreateAsync(
                RatingTargetType.ParkItem,
                category,
                token => this.BuildParkItemRanksAsync(category, token),
                cancellationToken);
            return ranks.TryGetValue(aggregate.TargetId, out int rank) ? rank : null;
        }

        return null;
    }

    public void Invalidate()
    {
        this.snapshotCache.Invalidate();
    }

    private async Task<IReadOnlyDictionary<string, int>> BuildParkRanksAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RatingRankingItemResult> sources =
            await this.ratingRepository.GetVisibleRankingSourcesAsync(
                null,
                RankingSourceLimit,
                cancellationToken);
        return RatingRankingFactory.BuildParkRankings(sources)
            .ToDictionary(
                static ranking => ranking.ParkId,
                static ranking => ranking.Rank,
                StringComparer.Ordinal);
    }

    private async Task<IReadOnlyDictionary<string, int>> BuildParkItemRanksAsync(
        ParkItemCategory category,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RatingRankingItemResult> sources =
            await this.ratingRepository.GetVisibleParkItemRankingSourcesAsync(
                category,
                RankingSourceLimit,
                cancellationToken);
        return RatingRankingFactory.BuildParkItemRankings(sources)
            .ToDictionary(
                static ranking => ranking.TargetId,
                static ranking => ranking.Rank,
                StringComparer.Ordinal);
    }
}
