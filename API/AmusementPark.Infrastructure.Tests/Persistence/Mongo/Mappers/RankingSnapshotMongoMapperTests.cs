using AmusementPark.Core.Domain.Parks;
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
            PublishedAtUtc,
            buildAttempt: 3);

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
        Assert.Equal(3, restored.BuildAttempt);
        Assert.Equal(3, document.BuildAttempt);
    }

    [Fact]
    public void ChunkDocument_ShouldRoundTripEvidenceWithoutRecalculation()
    {
        RankingSnapshotEntry entry = CreateEntry(1, "park-1");
        RankingSnapshotChunk chunk = new RankingSnapshotChunk(
            RankingSnapshotId.Parse("snapshot-1"),
            0,
            new[] { entry },
            RankingSnapshotChecksum.Parse(new string('b', 64)),
            buildAttempt: 2);

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
        Assert.Equal(2, restored.BuildAttempt);
        Assert.Equal(2, document.BuildAttempt);
    }

    [Fact]
    public void ChunkDocument_ShouldRoundTripTheParkItemCategory()
    {
        RankingEvidence evidence = CreateSimpleEvidence();
        RankingSnapshotEntry entry = new RankingSnapshotEntry(
            position: 1,
            rank: 1,
            RatingTargetType.ParkItem,
            "item-1",
            ParkItemCategory.Attraction,
            4.25d,
            evidence);
        RankingSnapshotChunk chunk = new RankingSnapshotChunk(
            RankingSnapshotId.Parse("snapshot-item"),
            0,
            new[] { entry },
            RankingSnapshotChecksum.Parse(new string('b', 64)));

        RankingSnapshotEntry restored = Assert.Single(
            chunk.ToDocument(GeneratedAtUtc).ToDomain(MethodologyVersion).Entries);

        Assert.Equal(ParkItemCategory.Attraction, restored.ParkItemCategory);
    }

    [Fact]
    public void PointerDocument_ShouldRoundTripOptimisticVersionAndPreviousSnapshot()
    {
        RankingPublicationPointer pointer = new RankingPublicationPointer(
            RankingScopeKey.Parse("parks:global"),
            RankingSnapshotId.Parse("snapshot-2"),
            RankingSnapshotId.Parse("snapshot-1"),
            GeneratedAtUtc,
            MethodologyVersion,
            43,
            50,
            7,
            PublishedAtUtc);

        RankingPublicationPointerDocument document = pointer.ToDocument("pointer-1", GeneratedAtUtc);
        RankingPublicationPointer restored = document.ToDomain();

        Assert.Equal(pointer.ScopeKey, restored.ScopeKey);
        Assert.Equal(pointer.CurrentSnapshotId, restored.CurrentSnapshotId);
        Assert.Equal(pointer.PreviousSnapshotId, restored.PreviousSnapshotId);
        Assert.Equal(
            pointer.PreviousSnapshotPublishedAtUtc,
            restored.PreviousSnapshotPublishedAtUtc);
        Assert.Equal(pointer.MethodologyVersion, restored.MethodologyVersion);
        Assert.Equal(pointer.SourceRevision, restored.SourceRevision);
        Assert.Equal(pointer.HighestPublishedSourceRevision, restored.HighestPublishedSourceRevision);
        Assert.Equal(pointer.Version, restored.Version);
        Assert.Equal(pointer.UpdatedAtUtc, restored.UpdatedAtUtc);
        Assert.Equal("pointer-1", document.Id);
        Assert.Equal(GeneratedAtUtc, document.CreatedAt);
    }

    [Fact]
    public void PointerDocument_WhenHighWaterFieldIsMissing_ShouldUseTheCurrentRevision()
    {
        RankingPublicationPointerDocument legacy = new RankingPublicationPointerDocument
        {
            Id = "pointer-legacy",
            ScopeKey = "parks:global",
            CurrentSnapshotId = "snapshot-1",
            PreviousSnapshotId = "snapshot-0",
            MethodologyVersion = MethodologyVersion.Value,
            SourceRevision = 43,
            Version = 2,
            CreatedAt = GeneratedAtUtc,
            UpdatedAt = PublishedAtUtc,
        };

        RankingPublicationPointer restored = legacy.ToDomain();

        Assert.Equal(43, restored.HighestPublishedSourceRevision);
        Assert.Equal(PublishedAtUtc, restored.PreviousSnapshotPublishedAtUtc);
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
        return new RankingSnapshotEntry(rank, RatingTargetType.Park, targetId, 4.25d, CreateEvidence());
    }

    private static RankingEvidence CreateEvidence()
    {
        return new RankingEvidence(
            RankingEvidenceLevel.Eligible,
            true,
            12,
            12,
            12,
            0,
            0,
            0,
            MethodologyVersion,
            null)
        {
            NextContributorThreshold = 30,
        };
    }

    private static RankingEvidence CreateSimpleEvidence()
    {
        return RankingEligibilityPolicy.Initial.EvaluateSimpleTarget(
            new SimpleRankingEvidenceInput(
                UniqueContributorCount: 12,
                RatingObservationCount: 12,
                TargetCanReceiveVisitorRatings: true,
                IsExcludedByModeration: false,
                AggregateIntegrityIsValid: true));
    }
}
