using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class RankingSnapshotMongoMapperTests
{
    private static readonly DateTime GeneratedAtUtc = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ValidatedAtUtc = GeneratedAtUtc.AddMinutes(1);
    private static readonly DateTime PublishedAtUtc = GeneratedAtUtc.AddMinutes(2);
    private static readonly RatingMethodologyVersion MethodologyVersion =
        RatingMethodologyVersion.Parse("ratings-2026-01");

    [Fact]
    public void HeaderDocument_ShouldRoundTripAValidatedLifecycle()
    {
        RankingSnapshotHeader header = new RankingSnapshotHeader(
            RankingSnapshotId.Parse("snapshot-1"),
            RankingScopeKey.Parse("parks:global"),
            MethodologyVersion,
            42,
            RankingSnapshotStatus.Current,
            502,
            501,
            500,
            2,
            RankingSnapshotChecksum.Parse(new string('a', 64)),
            GeneratedAtUtc,
            ValidatedAtUtc,
            PublishedAtUtc);

        RankingSnapshotHeaderDocument document = header.ToDocument(GeneratedAtUtc);
        RankingSnapshotHeader restored = document.ToDomain();

        Assert.Equal(header.Id, restored.Id);
        Assert.Equal(header.ScopeKey, restored.ScopeKey);
        Assert.Equal(header.MethodologyVersion, restored.MethodologyVersion);
        Assert.Equal(header.SourceRevision, restored.SourceRevision);
        Assert.Equal(header.Status, restored.Status);
        Assert.Equal(header.EligibleEntryCount, restored.EligibleEntryCount);
        Assert.Equal(header.Checksum, restored.Checksum);
        Assert.Equal(PublishedAtUtc, restored.PublishedAtUtc);
    }

    [Fact]
    public void ChunkDocument_ShouldRoundTripEvidenceWithoutRecalculation()
    {
        RankingSnapshotEntry entry = CreateEntry(1, "park-1");
        RankingSnapshotChunk chunk = new RankingSnapshotChunk(
            RankingSnapshotId.Parse("snapshot-1"),
            0,
            new[] { entry },
            RankingSnapshotChecksum.Parse(new string('b', 64)));

        RankingSnapshotChunkDocument document = chunk.ToDocument(GeneratedAtUtc);
        RankingSnapshotChunk restored = document.ToDomain(MethodologyVersion);

        RankingSnapshotEntry restoredEntry = Assert.Single(restored.Entries);
        Assert.Equal(chunk.SnapshotId, restored.SnapshotId);
        Assert.Equal(chunk.ChunkIndex, restored.ChunkIndex);
        Assert.Equal(chunk.Checksum, restored.Checksum);
        Assert.Equal(entry.Position, restoredEntry.Position);
        Assert.Equal(entry.TargetId, restoredEntry.TargetId);
        Assert.Equal(entry.Score, restoredEntry.Score);
        Assert.Equal(entry.Evidence, restoredEntry.Evidence);
    }

    [Fact]
    public void PointerDocument_ShouldRoundTripOptimisticVersionAndPreviousSnapshot()
    {
        RankingPublicationPointer pointer = new RankingPublicationPointer(
            RankingScopeKey.Parse("parks:global"),
            RankingSnapshotId.Parse("snapshot-2"),
            RankingSnapshotId.Parse("snapshot-1"),
            MethodologyVersion,
            43,
            7,
            PublishedAtUtc);

        RankingPublicationPointerDocument document = pointer.ToDocument("pointer-1", GeneratedAtUtc);
        RankingPublicationPointer restored = document.ToDomain();

        Assert.Equal(pointer.ScopeKey, restored.ScopeKey);
        Assert.Equal(pointer.CurrentSnapshotId, restored.CurrentSnapshotId);
        Assert.Equal(pointer.PreviousSnapshotId, restored.PreviousSnapshotId);
        Assert.Equal(pointer.MethodologyVersion, restored.MethodologyVersion);
        Assert.Equal(pointer.SourceRevision, restored.SourceRevision);
        Assert.Equal(pointer.Version, restored.Version);
        Assert.Equal(pointer.UpdatedAtUtc, restored.UpdatedAtUtc);
        Assert.Equal("pointer-1", document.Id);
        Assert.Equal(GeneratedAtUtc, document.CreatedAt);
    }

    [Fact]
    public void Documents_ShouldSerializeLifecycleEnumsAsStableStrings()
    {
        RankingSnapshotHeaderDocument document = new RankingSnapshotHeaderDocument
        {
            Status = RankingSnapshotStatus.Validated,
        };

        BsonDocument serialized = document.ToBsonDocument();

        Assert.Equal("Validated", serialized["status"].AsString);
    }

    private static RankingSnapshotEntry CreateEntry(int rank, string targetId)
    {
        RankingEvidence evidence = new RankingEvidence(
            RankingEvidenceLevel.Established,
            true,
            8,
            10,
            8,
            0,
            0,
            0,
            MethodologyVersion,
            null)
        {
            NextContributorThreshold = 15,
        };
        return new RankingSnapshotEntry(rank, RatingTargetType.Park, targetId, 4.25d, evidence);
    }
}
