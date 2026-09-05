using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class CanonicalParkItemRatingRankingReader : ICanonicalParkItemRatingRankingReader
{
    private readonly IRatingRankProvider ratingRankProvider;
    private readonly IRatingRepository ratingRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public CanonicalParkItemRatingRankingReader(
        IRatingRankProvider ratingRankProvider,
        IRatingRepository ratingRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.ratingRankProvider = ratingRankProvider;
        this.ratingRepository = ratingRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<PagedResult<ParkItemRatingRankingResult>> ReadAsync(
        ParkItemCategory category,
        int page,
        int pageSize,
        string? search,
        ParkItemType? parkItemType,
        CancellationToken cancellationToken)
    {
        RatingPublishedRankingSnapshot? snapshot = await this.ratingRankProvider.GetCanonicalSnapshotAsync(
            RatingTargetType.ParkItem,
            category,
            cancellationToken);
        if (snapshot is null)
        {
            return new PagedResult<ParkItemRatingRankingResult>(
                Array.Empty<ParkItemRatingRankingResult>(),
                page,
                pageSize,
                0);
        }

        bool requiresMetadataFilter = parkItemType.HasValue || !string.IsNullOrWhiteSpace(search);
        IReadOnlyCollection<RankingSnapshotEntry> filteredEntries = snapshot.Entries;
        IReadOnlyDictionary<string, ParkItemRankingMetadata>? filteredMetadata = null;
        if (requiresMetadataFilter)
        {
            filteredMetadata = await this.LoadVisibleMetadataAsync(
                snapshot.Entries,
                category,
                cancellationToken);
            string? normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            filteredEntries = snapshot.Entries
                .Where(entry => filteredMetadata.TryGetValue(
                    entry.TargetId,
                    out ParkItemRankingMetadata? metadata)
                    && (!parkItemType.HasValue || metadata.Item.Type == parkItemType.Value)
                    && (normalizedSearch is null
                        || metadata.Item.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                        || metadata.ParkName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        IReadOnlyCollection<RankingSnapshotEntry> selectedEntries = filteredEntries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        IReadOnlyDictionary<string, ParkItemRankingMetadata> selectedMetadata = filteredMetadata
            ?? await this.LoadVisibleMetadataAsync(selectedEntries, category, cancellationToken);
        IReadOnlyCollection<ParkItemRatingRankingResult> items = await this.HydrateAsync(
            snapshot,
            selectedEntries,
            selectedMetadata,
            category,
            parkItemType.HasValue,
            cancellationToken);
        RatingPublishedRankingSnapshot? currentSnapshot =
            await this.ratingRankProvider.GetCanonicalSnapshotAsync(
                RatingTargetType.ParkItem,
                category,
                cancellationToken);
        if (items.Count != selectedEntries.Count
            || currentSnapshot is null
            || currentSnapshot.SnapshotId != snapshot.SnapshotId
            || currentSnapshot.MethodologyVersion != snapshot.MethodologyVersion
            || currentSnapshot.SourceRevision != snapshot.SourceRevision
            || currentSnapshot.PointerVersion != snapshot.PointerVersion)
        {
            return new PagedResult<ParkItemRatingRankingResult>(
                Array.Empty<ParkItemRatingRankingResult>(),
                page,
                pageSize,
                0);
        }

        return new PagedResult<ParkItemRatingRankingResult>(
            items,
            page,
            pageSize,
            filteredEntries.Count);
    }

    private async Task<IReadOnlyDictionary<string, ParkItemRankingMetadata>> LoadVisibleMetadataAsync(
        IReadOnlyCollection<RankingSnapshotEntry> entries,
        ParkItemCategory category,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return new Dictionary<string, ParkItemRankingMetadata>(StringComparer.Ordinal);
        }

        IReadOnlyCollection<ParkItem> parkItems = await this.parkItemRepository.GetByIdsAsync(
            entries.Select(static entry => entry.TargetId).ToList(),
            cancellationToken);
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(
            parkItems.Select(static item => item.ParkId),
            cancellationToken);
        IReadOnlyDictionary<string, Park> parksById = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .GroupBy(static park => park.Id, StringComparer.Ordinal)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.Single(), StringComparer.Ordinal);
        Dictionary<string, ParkItemRankingMetadata> metadataByTargetId =
            new Dictionary<string, ParkItemRankingMetadata>(StringComparer.Ordinal);
        foreach (ParkItem item in parkItems)
        {
            if (string.IsNullOrWhiteSpace(item.Id)
                || string.IsNullOrWhiteSpace(item.ParkId)
                || !item.IsVisible
                || item.Category != category
                || !parksById.TryGetValue(item.ParkId, out Park? park)
                || !park.IsVisible
                || !park.Status.CanReceiveVisitorRatings()
                || !ParkItemStatusNormalizer.CanReceiveVisitorRatings(
                    item.Category,
                    item.AttractionDetails?.Status))
            {
                continue;
            }

            _ = metadataByTargetId.TryAdd(
                item.Id,
                new ParkItemRankingMetadata(item, park.Name?.Trim() ?? park.Id));
        }

        return metadataByTargetId;
    }

    private async Task<IReadOnlyCollection<ParkItemRatingRankingResult>> HydrateAsync(
        RatingPublishedRankingSnapshot snapshot,
        IReadOnlyCollection<RankingSnapshotEntry> entries,
        IReadOnlyDictionary<string, ParkItemRankingMetadata> metadataByTargetId,
        ParkItemCategory category,
        bool withholdRank,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RatingAggregate> aggregates = await this.ratingRepository.GetAggregatesAsync(
            RatingTargetType.ParkItem,
            entries.Select(static entry => entry.TargetId).ToList(),
            cancellationToken);
        IReadOnlyDictionary<string, RatingAggregate> aggregatesByTargetId = aggregates
            .Where(aggregate => aggregate.TargetType == RatingTargetType.ParkItem
                && aggregate.ParkItemCategory == category
                && aggregate.RatingCount > 0
                && aggregate.SourceIntegrityIsValid == true)
            .GroupBy(static aggregate => aggregate.TargetId, StringComparer.Ordinal)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.Single(), StringComparer.Ordinal);
        List<ParkItemRatingRankingResult> items = new List<ParkItemRatingRankingResult>();
        foreach (RankingSnapshotEntry entry in entries)
        {
            if (!metadataByTargetId.TryGetValue(entry.TargetId, out ParkItemRankingMetadata? metadata)
                || !aggregatesByTargetId.TryGetValue(entry.TargetId, out RatingAggregate? aggregate))
            {
                continue;
            }

            items.Add(new ParkItemRatingRankingResult(
                withholdRank ? null : entry.Rank,
                entry.TargetId,
                metadata.Item.Name,
                metadata.Item.ParkId,
                metadata.ParkName,
                category,
                metadata.Item.Type,
                aggregate.RatingCount,
                aggregate.AverageRating,
                entry.Score)
            {
                Evidence = RatingResultFactory.ToResult(entry.Evidence),
                GeneratedAtUtc = snapshot.GeneratedAtUtc,
            });
        }

        return items;
    }
}
