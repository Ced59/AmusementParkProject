using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class CanonicalParkRatingRankingReader : ICanonicalParkRatingRankingReader
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRankProvider ratingRankProvider;
    private readonly IRatingRepository ratingRepository;
    private readonly IParkRepository parkRepository;

    public CanonicalParkRatingRankingReader(
        IRatingRankProvider ratingRankProvider,
        IRatingRepository ratingRepository,
        IParkRepository parkRepository)
    {
        this.ratingRankProvider = ratingRankProvider;
        this.ratingRepository = ratingRepository;
        this.parkRepository = parkRepository;
    }

    public async Task<PagedResult<ParkRatingRankingResult>> ReadAsync(
        int page,
        int pageSize,
        string? parkSearch,
        CancellationToken cancellationToken)
    {
        RatingPublishedRankingSnapshot? snapshot = await this.ratingRankProvider.GetCanonicalSnapshotAsync(
            RatingTargetType.Park,
            null,
            cancellationToken);
        if (snapshot is null)
        {
            return new PagedResult<ParkRatingRankingResult>(
                Array.Empty<ParkRatingRankingResult>(),
                page,
                pageSize,
                0);
        }

        IReadOnlyCollection<RankingSnapshotEntry> selectedEntries;
        long totalItems;
        int resultPage;
        int resultPageSize;
        if (string.IsNullOrWhiteSpace(parkSearch))
        {
            selectedEntries = snapshot.Entries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            totalItems = snapshot.Entries.Count;
            resultPage = page;
            resultPageSize = pageSize;
        }
        else
        {
            selectedEntries = await this.BuildSearchWindowAsync(
                snapshot.Entries,
                parkSearch.Trim(),
                cancellationToken);
            totalItems = selectedEntries.Count;
            resultPage = 1;
            resultPageSize = Math.Max(selectedEntries.Count, 1);
        }

        IReadOnlyCollection<ParkRatingRankingResult> items = await this.HydrateAsync(
            snapshot,
            selectedEntries,
            cancellationToken);
        if (items.Count != selectedEntries.Count
            || !await this.IsStillCurrentAsync(snapshot, cancellationToken))
        {
            return new PagedResult<ParkRatingRankingResult>(
                Array.Empty<ParkRatingRankingResult>(),
                resultPage,
                resultPageSize,
                0);
        }

        return new PagedResult<ParkRatingRankingResult>(
            items,
            resultPage,
            resultPageSize,
            totalItems);
    }

    private async Task<bool> IsStillCurrentAsync(
        RatingPublishedRankingSnapshot expected,
        CancellationToken cancellationToken)
    {
        RatingPublishedRankingSnapshot? current = await this.ratingRankProvider.GetCanonicalSnapshotAsync(
            RatingTargetType.Park,
            null,
            cancellationToken);
        return current is not null
            && current.SnapshotId == expected.SnapshotId
            && current.MethodologyVersion == expected.MethodologyVersion
            && current.SourceRevision == expected.SourceRevision
            && current.PointerVersion == expected.PointerVersion;
    }

    private async Task<IReadOnlyCollection<RankingSnapshotEntry>> BuildSearchWindowAsync(
        IReadOnlyCollection<RankingSnapshotEntry> entries,
        string parkSearch,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(
            entries.Select(static entry => entry.TargetId),
            cancellationToken);
        IReadOnlyDictionary<string, Park> parksById = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .GroupBy(static park => park.Id, StringComparer.Ordinal)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.Single(), StringComparer.Ordinal);
        List<RankingSnapshotEntry> searchableEntries = entries
            .Where(entry => parksById.TryGetValue(entry.TargetId, out Park? park)
                && park.IsVisible
                && park.Status.CanReceiveVisitorRatings())
            .ToList();
        int matchIndex = searchableEntries.FindIndex(entry =>
            parksById[entry.TargetId].Name?.Contains(
                parkSearch,
                StringComparison.OrdinalIgnoreCase) == true);
        if (matchIndex < 0)
        {
            return Array.Empty<RankingSnapshotEntry>();
        }

        const int contextSize = 5;
        int startIndex = Math.Max(0, matchIndex - contextSize);
        int endIndex = Math.Min(searchableEntries.Count - 1, matchIndex + contextSize);
        return searchableEntries
            .Skip(startIndex)
            .Take(endIndex - startIndex + 1)
            .ToList();
    }

    private async Task<IReadOnlyCollection<ParkRatingRankingResult>> HydrateAsync(
        RatingPublishedRankingSnapshot snapshot,
        IReadOnlyCollection<RankingSnapshotEntry> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return Array.Empty<ParkRatingRankingResult>();
        }

        RatingRankingSourceBatch sourceBatch =
            await this.ratingRepository.GetVisibleParkRankingSnapshotSourceBatchAsync(
                entries.Select(static entry => entry.TargetId).ToList(),
                RankingSourceLimit,
                cancellationToken);
        if (sourceBatch.IsTruncated)
        {
            return Array.Empty<ParkRatingRankingResult>();
        }

        HashSet<string> validParkIds = sourceBatch.Sources
            .GroupBy(static source => source.ParkId, StringComparer.Ordinal)
            .Where(static group => group.All(source => source.AggregateIntegrityIsValid == true))
            .Select(static group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        IReadOnlyCollection<RatingRankingItemResult> validSources = sourceBatch.Sources
            .Where(source => validParkIds.Contains(source.ParkId))
            .ToList();
        IReadOnlyDictionary<string, ParkRatingRankingResult> rankingsByParkId =
            RatingRankingFactory.BuildParkRankings(validSources)
                .GroupBy(static ranking => ranking.ParkId, StringComparer.Ordinal)
                .Where(static group => group.Count() == 1)
                .ToDictionary(static group => group.Key, static group => group.Single(), StringComparer.Ordinal);
        List<ParkRatingRankingResult> rankings = new List<ParkRatingRankingResult>();
        foreach (RankingSnapshotEntry entry in entries)
        {
            if (!rankingsByParkId.TryGetValue(entry.TargetId, out ParkRatingRankingResult? ranking))
            {
                continue;
            }

            rankings.Add(ranking with
            {
                Rank = entry.Rank,
                Score = entry.Score,
                Evidence = RatingResultFactory.ToResult(entry.Evidence),
                GeneratedAtUtc = snapshot.GeneratedAtUtc,
            });
        }

        return rankings;
    }
}
