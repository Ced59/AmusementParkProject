using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class RankingSnapshotMongoMapper
{
    public static RankingSnapshotHeader ToDomain(this RankingSnapshotHeaderDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new RankingSnapshotHeader(
            RankingSnapshotId.Parse(document.Id),
            RankingScopeKey.Parse(document.ScopeKey),
            RatingMethodologyVersion.Parse(document.MethodologyVersion),
            document.SourceRevision,
            document.Status,
            document.TotalEntryCount,
            document.EligibleEntryCount,
            document.ChunkSize,
            document.ChunkCount,
            RankingSnapshotChecksum.Parse(document.Checksum),
            EnsureUtc(document.GeneratedAtUtc),
            EnsureOptionalUtc(document.ValidatedAtUtc),
            EnsureOptionalUtc(document.PublishedAtUtc),
            document.FailureCode,
            NormalizeBuildAttempt(document.BuildAttempt));
    }

    public static RankingSnapshotChunk ToDomain(
        this RankingSnapshotChunkDocument document,
        RatingMethodologyVersion methodologyVersion)
    {
        ArgumentNullException.ThrowIfNull(document);
        List<RankingSnapshotEntry> entries = document.Entries
            .Select(entry => entry.ToDomain(methodologyVersion))
            .ToList();
        return new RankingSnapshotChunk(
            RankingSnapshotId.Parse(document.SnapshotId),
            document.ChunkIndex,
            entries,
            RankingSnapshotChecksum.Parse(document.Checksum),
            NormalizeBuildAttempt(document.BuildAttempt));
    }

    public static RankingPublicationPointer ToDomain(this RankingPublicationPointerDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new RankingPublicationPointer(
            RankingScopeKey.Parse(document.ScopeKey),
            RankingSnapshotId.Parse(document.CurrentSnapshotId),
            ResolveCurrentSnapshotPublishedAt(document),
            string.IsNullOrWhiteSpace(document.PreviousSnapshotId)
                ? null
                : RankingSnapshotId.Parse(document.PreviousSnapshotId),
            ResolvePreviousSnapshotPublishedAt(document),
            RatingMethodologyVersion.Parse(document.MethodologyVersion),
            document.SourceRevision,
            Math.Max(document.SourceRevision, document.HighestPublishedSourceRevision),
            document.Version,
            EnsureUtc(document.UpdatedAt));
    }

    public static RankingSnapshotChunkDocument ToDocument(
        this RankingSnapshotChunk chunk,
        RankingScopeKey scopeKey,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        return new RankingSnapshotChunkDocument
        {
            Id = $"{chunk.SnapshotId.Value}:{chunk.ChunkIndex}",
            ScopeKey = scopeKey.Value,
            SnapshotId = chunk.SnapshotId.Value,
            ChunkIndex = chunk.ChunkIndex,
            FirstRank = chunk.FirstRank,
            LastRank = chunk.LastRank,
            FirstPosition = chunk.FirstPosition,
            LastPosition = chunk.LastPosition,
            EntryCount = chunk.Entries.Count,
            BuildAttempt = chunk.BuildAttempt,
            Checksum = chunk.Checksum.Value,
            Entries = chunk.Entries.Select(static entry => entry.ToDocument()).ToList(),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
        };
    }

    public static RankingSnapshotHeaderDocument ToDocument(
        this RankingSnapshotHeader header,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(header);
        return new RankingSnapshotHeaderDocument
        {
            Id = header.Id.Value,
            ScopeKey = header.ScopeKey.Value,
            MethodologyVersion = header.MethodologyVersion.Value,
            SourceRevision = header.SourceRevision,
            Status = header.Status,
            TotalEntryCount = header.TotalEntryCount,
            EligibleEntryCount = header.EligibleEntryCount,
            ChunkSize = header.ChunkSize,
            ChunkCount = header.ChunkCount,
            Checksum = header.Checksum.Value,
            GeneratedAtUtc = header.GeneratedAtUtc,
            ValidatedAtUtc = header.ValidatedAtUtc,
            PublishedAtUtc = header.PublishedAtUtc,
            FailureCode = header.FailureCode,
            BuildAttempt = header.BuildAttempt,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
        };
    }

    public static RankingPublicationPointerDocument ToDocument(
        this RankingPublicationPointer pointer,
        string documentId,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        return new RankingPublicationPointerDocument
        {
            Id = documentId,
            ScopeKey = pointer.ScopeKey.Value,
            CurrentSnapshotId = pointer.CurrentSnapshotId.Value,
            CurrentSnapshotPublishedAtUtc = pointer.CurrentSnapshotPublishedAtUtc,
            PreviousSnapshotId = pointer.PreviousSnapshotId?.Value,
            PreviousSnapshotPublishedAtUtc = pointer.PreviousSnapshotPublishedAtUtc,
            MethodologyVersion = pointer.MethodologyVersion.Value,
            SourceRevision = pointer.SourceRevision,
            HighestPublishedSourceRevision = pointer.HighestPublishedSourceRevision,
            Version = pointer.Version,
            CreatedAt = createdAtUtc,
            UpdatedAt = pointer.UpdatedAtUtc,
        };
    }

    private static RankingSnapshotEntry ToDomain(
        this RankingSnapshotEntryDocument document,
        RatingMethodologyVersion methodologyVersion)
    {
        RankingEvidence evidence = new RankingEvidence(
            document.EvidenceLevel,
            true,
            document.UniqueContributorCount,
            document.RatingObservationCount,
            document.DirectParkContributorCount,
            document.ItemContributorCount,
            document.EligibleItemCount,
            document.EligibleCategoryCount,
            methodologyVersion,
            null)
        {
            NextContributorThreshold = document.NextContributorThreshold,
            IsSingleCategoryParkException = document.IsSingleCategoryParkException,
        };
        return new RankingSnapshotEntry(
            document.Position,
            document.Rank,
            document.TargetType,
            document.TargetId,
            document.ParkItemCategory,
            document.Score,
            evidence);
    }

    private static RankingSnapshotEntryDocument ToDocument(this RankingSnapshotEntry entry)
    {
        return new RankingSnapshotEntryDocument
        {
            Position = entry.Position,
            Rank = entry.Rank,
            TargetType = entry.TargetType,
            TargetId = entry.TargetId,
            ParkItemCategory = entry.ParkItemCategory,
            Score = entry.Score,
            EvidenceLevel = entry.Evidence.Level,
            UniqueContributorCount = entry.Evidence.UniqueContributorCount,
            RatingObservationCount = entry.Evidence.RatingObservationCount,
            DirectParkContributorCount = entry.Evidence.DirectParkContributorCount,
            ItemContributorCount = entry.Evidence.ItemContributorCount,
            EligibleItemCount = entry.Evidence.EligibleItemCount,
            EligibleCategoryCount = entry.Evidence.EligibleCategoryCount,
            IsSingleCategoryParkException = entry.Evidence.IsSingleCategoryParkException,
            NextContributorThreshold = entry.Evidence.NextContributorThreshold,
        };
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? EnsureOptionalUtc(DateTime? value)
    {
        return value.HasValue ? EnsureUtc(value.Value) : null;
    }

    private static DateTime? ResolvePreviousSnapshotPublishedAt(
        RankingPublicationPointerDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.PreviousSnapshotId))
        {
            return null;
        }

        return EnsureOptionalUtc(document.PreviousSnapshotPublishedAtUtc) ?? EnsureUtc(document.UpdatedAt);
    }

    private static DateTime ResolveCurrentSnapshotPublishedAt(
        RankingPublicationPointerDocument document)
    {
        return EnsureOptionalUtc(document.CurrentSnapshotPublishedAtUtc) ?? EnsureUtc(document.UpdatedAt);
    }

    private static int NormalizeBuildAttempt(int buildAttempt)
    {
        return Math.Max(1, buildAttempt);
    }
}
