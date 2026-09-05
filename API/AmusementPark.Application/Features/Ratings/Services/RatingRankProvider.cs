using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankProvider : IRatingRankProvider
{
    private readonly IRatingRankSnapshotCache snapshotCache;
    private readonly IRankingSnapshotRepository rankingSnapshotRepository;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IRankingScopeRegistry scopeRegistry;
    private readonly RankingSnapshotChecksumCalculator checksumCalculator;
    private readonly RankingSnapshotIntegrityValidator integrityValidator;

    public RatingRankProvider(
        IRatingRankSnapshotCache snapshotCache,
        IRankingSnapshotRepository rankingSnapshotRepository,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRankingScopeRegistry scopeRegistry,
        RankingSnapshotChecksumCalculator checksumCalculator,
        RankingSnapshotIntegrityValidator integrityValidator)
    {
        this.snapshotCache = snapshotCache;
        this.rankingSnapshotRepository = rankingSnapshotRepository;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.scopeRegistry = scopeRegistry;
        this.checksumCalculator = checksumCalculator;
        this.integrityValidator = integrityValidator;
    }

    public async Task<RatingPublishedRank?> GetRankAsync(
        RatingAggregate aggregate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        RatingPublishedRankingSnapshot? snapshot = await this.GetCanonicalSnapshotAsync(
            aggregate.TargetType,
            aggregate.ParkItemCategory,
            cancellationToken);
        RankingSnapshotEntry? entry = snapshot?.FindEntry(aggregate.TargetId);
        if (entry is null || snapshot is null || entry.TargetType != aggregate.TargetType)
        {
            return null;
        }

        return new RatingPublishedRank(
            entry.Rank,
            snapshot.MethodologyVersion,
            snapshot.GeneratedAtUtc);
    }

    public async Task<RatingPublishedRankingSnapshot?> GetCanonicalSnapshotAsync(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory,
        CancellationToken cancellationToken)
    {
        RankingScopeDefinition? scope = this.ResolveScope(targetType, parkItemCategory);
        if (scope is null)
        {
            return null;
        }

        PublishedSnapshotState? initialState = await this.ReadCurrentStateAsync(scope, cancellationToken);
        if (initialState is null)
        {
            return null;
        }

        RatingPublishedRankingSnapshot? snapshot = await this.snapshotCache.GetOrCreatePublishedAsync(
            scope.Key,
            initialState.Pointer.CurrentSnapshotId,
            scope.MethodologyVersion,
            initialState.SourceRevision,
            initialState.Pointer.Version,
            token => this.LoadAndValidateSnapshotAsync(scope, initialState, token),
            cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        PublishedSnapshotState? finalState = await this.ReadCurrentStateAsync(scope, cancellationToken);
        return finalState is not null && StatesMatch(initialState, finalState)
            ? snapshot
            : null;
    }

    public void Invalidate()
    {
        this.snapshotCache.Invalidate();
    }

    private async Task<PublishedSnapshotState?> ReadCurrentStateAsync(
        RankingScopeDefinition scope,
        CancellationToken cancellationToken)
    {
        RatingRankingSourceRevision? revision = await this.sourceRevisionRepository.GetAsync(
            scope.Key,
            cancellationToken);
        if (revision is null
            || revision.ScopeKey != scope.Key
            || !revision.IsRebuildable
            || revision.CoversUnavailable(scope.MethodologyVersion, revision.Revision))
        {
            return null;
        }

        long sourceRevision = revision.Revision;

        RankingPublicationPointer? pointer = await this.rankingSnapshotRepository.GetPointerAsync(
            scope.Key,
            cancellationToken);
        if (pointer is null
            || pointer.ScopeKey != scope.Key
            || pointer.MethodologyVersion != scope.MethodologyVersion
            || pointer.SourceRevision != sourceRevision
            || pointer.HighestPublishedSourceRevision < sourceRevision)
        {
            return null;
        }

        return new PublishedSnapshotState(sourceRevision, pointer);
    }

    private async Task<RatingPublishedRankingSnapshot?> LoadAndValidateSnapshotAsync(
        RankingScopeDefinition scope,
        PublishedSnapshotState state,
        CancellationToken cancellationToken)
    {
        RankingSnapshotHeader? header = await this.rankingSnapshotRepository.GetCurrentHeaderAsync(
            scope.Key,
            scope.MethodologyVersion,
            cancellationToken);
        if (header is null
            || header.Id != state.Pointer.CurrentSnapshotId
            || header.ScopeKey != scope.Key
            || header.MethodologyVersion != scope.MethodologyVersion
            || header.SourceRevision != state.SourceRevision
            || header.Status is not (
                RankingSnapshotStatus.Validated
                or RankingSnapshotStatus.Current
                or RankingSnapshotStatus.Superseded)
            || !scope.EvaluatePublication(header.EligibleEntryCount).IsEligible)
        {
            return null;
        }

        List<RankingSnapshotChunk> chunks = new List<RankingSnapshotChunk>(header.ChunkCount);
        List<RankingSnapshotEntry> entries = new List<RankingSnapshotEntry>(header.EligibleEntryCount);
        for (int chunkIndex = 0; chunkIndex < header.ChunkCount; chunkIndex++)
        {
            int offset = chunkIndex * header.ChunkSize;
            RankingSnapshotPage? page = await this.rankingSnapshotRepository.GetCurrentPageAsync(
                scope.Key,
                scope.MethodologyVersion,
                offset,
                header.ChunkSize,
                cancellationToken);
            if (page is null
                || page.Offset != offset
                || page.Limit != header.ChunkSize
                || !HeadersMatch(header, page.Header))
            {
                return null;
            }

            RankingSnapshotEntry[] chunkEntries = page.Entries.ToArray();
            int expectedEntryCount = chunkIndex == header.ChunkCount - 1
                ? header.EligibleEntryCount - offset
                : header.ChunkSize;
            if (chunkEntries.Length != expectedEntryCount)
            {
                return null;
            }

            RankingSnapshotChunk chunk = new RankingSnapshotChunk(
                header.Id,
                chunkIndex,
                chunkEntries,
                this.checksumCalculator.CalculateChunk(chunkEntries),
                header.BuildAttempt);
            chunks.Add(chunk);
            entries.AddRange(chunkEntries);
        }

        RankingSnapshotIntegrityResult integrity = this.integrityValidator.Validate(header, chunks, scope);
        if (!integrity.IsValid)
        {
            return null;
        }

        return new RatingPublishedRankingSnapshot(
            scope.Key,
            header.Id,
            header.MethodologyVersion,
            header.SourceRevision,
            state.Pointer.Version,
            header.GeneratedAtUtc,
            Array.AsReadOnly(entries.ToArray()));
    }

    private RankingScopeDefinition? ResolveScope(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory)
    {
        if (targetType == RatingTargetType.Park && !parkItemCategory.HasValue)
        {
            return this.scopeRegistry.Definitions.SingleOrDefault(static definition =>
                definition.TargetFamily == RankingTargetFamily.Parks
                && definition.Filter.Kind == RankingScopeFilterKind.Global);
        }

        if (targetType != RatingTargetType.ParkItem
            || !parkItemCategory.HasValue
            || !Enum.IsDefined(parkItemCategory.Value))
        {
            return null;
        }

        return this.scopeRegistry.Definitions.SingleOrDefault(definition =>
            definition.TargetFamily == RankingTargetFamily.ParkItems
            && definition.Filter.Kind == RankingScopeFilterKind.ParkItemCategory
            && definition.Filter.ParkItemCategory == parkItemCategory.Value);
    }

    private static bool StatesMatch(PublishedSnapshotState left, PublishedSnapshotState right)
    {
        return left.SourceRevision == right.SourceRevision
            && left.Pointer.CurrentSnapshotId == right.Pointer.CurrentSnapshotId
            && left.Pointer.MethodologyVersion == right.Pointer.MethodologyVersion
            && left.Pointer.SourceRevision == right.Pointer.SourceRevision
            && left.Pointer.Version == right.Pointer.Version;
    }

    private static bool HeadersMatch(RankingSnapshotHeader expected, RankingSnapshotHeader actual)
    {
        return expected.Id == actual.Id
            && expected.ScopeKey == actual.ScopeKey
            && expected.MethodologyVersion == actual.MethodologyVersion
            && expected.SourceRevision == actual.SourceRevision
            && expected.Status == actual.Status
            && expected.TotalEntryCount == actual.TotalEntryCount
            && expected.EligibleEntryCount == actual.EligibleEntryCount
            && expected.ChunkSize == actual.ChunkSize
            && expected.ChunkCount == actual.ChunkCount
            && expected.Checksum == actual.Checksum
            && expected.GeneratedAtUtc == actual.GeneratedAtUtc
            && expected.BuildAttempt == actual.BuildAttempt;
    }
}
