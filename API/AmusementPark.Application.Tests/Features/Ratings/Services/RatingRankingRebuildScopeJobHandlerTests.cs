using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingRebuildScopeJobHandlerTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly RankingSnapshotChecksum PlaceholderChecksum =
        RankingSnapshotChecksum.Parse(new string('0', RankingSnapshotChecksum.HexadecimalLength));

    [Fact]
    public void Definition_ShouldUseSingleHeavyWorkerAndVersionedPayload()
    {
        HandlerFixture fixture = new HandlerFixture();

        DurableBackgroundJobHandlerDefinition definition = fixture.Handler.Definition;

        Assert.Equal(RatingRankingRebuildScopeJob.Kind, definition.Kind);
        Assert.Equal(DurableBackgroundJobWorkload.Heavy, definition.Workload);
        Assert.Equal(1, definition.MaximumConcurrency);
        Assert.True(definition.SupportsPayloadVersion(RatingRankingRebuildScopeJob.PayloadVersion));
    }

    [Fact]
    public async Task HandleAsync_WhenPayloadVersionIsUnsupported_ShouldDeadLetterWithoutReadingState()
    {
        HandlerFixture fixture = new HandlerFixture();
        DurableBackgroundJobExecutionContext context = fixture.CreateContext(4) with
        {
            PayloadVersion = 99,
        };

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            context,
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.DeadLetter, result.Outcome);
        Assert.Equal(RatingRankingRebuildErrorCodes.InvalidPayload, result.ErrorCode);
        fixture.VerifyNoCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPublishedPointerAlreadyCoversRevision_ShouldSkipReplay()
    {
        HandlerFixture fixture = new HandlerFixture();
        fixture.Snapshots
            .Setup(repository => repository.GetPointerAsync(fixture.Scope.Key, CancellationToken.None))
            .ReturnsAsync(fixture.CreatePointer(7));

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(7),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyNoOtherCalls();
        fixture.Builder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenForcedAndPointerCoversRevision_ShouldVerifyAndRepublishCurrentSnapshot()
    {
        HandlerFixture fixture = new HandlerFixture();
        IReadOnlyCollection<RankingSnapshotEntry> entries = fixture.CreateEligibleEntries();
        fixture.Snapshots
            .Setup(repository => repository.GetPointerAsync(fixture.Scope.Key, CancellationToken.None))
            .ReturnsAsync(fixture.CreatePointer(6));
        fixture.Revisions
            .Setup(repository => repository.GetAsync(fixture.Scope.Key, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(fixture.Scope.Key, 6, NowUtc));
        fixture.Revisions
            .Setup(repository => repository.GetAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                CanonicalRankingScopes.GlobalParks.Key,
                20,
                NowUtc));
        fixture.Builder
            .Setup(builder => builder.BuildAsync(fixture.Scope, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSnapshotBuildPlan(entries.Count, entries, false));
        fixture.Snapshots
            .Setup(repository => repository.StartBuildAsync(
                It.Is<StartRankingSnapshotBuildRequest>(request =>
                    request.SourceRevision == 6
                    && request.EligibleEntryCount == entries.Count),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotBuildStartResult(
                RankingSnapshotBuildStartDisposition.Existing,
                fixture.CreateHeader(RankingSnapshotStatus.Current, sourceRevision: 6)));
        fixture.Snapshots
            .Setup(repository => repository.PublishAsync(
                RankingSnapshotId.Parse("snapshot-1"),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotPublicationResult(
                RankingSnapshotPublicationDisposition.AlreadyPublished,
                fixture.CreatePointer(6)));

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(6, forceRebuild: true),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyAll();
        fixture.Builder.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenNewerRevisionExists_ShouldFenceStaleBuildBeforeReadingSources()
    {
        HandlerFixture fixture = new HandlerFixture();
        fixture.Snapshots
            .Setup(repository => repository.GetPointerAsync(fixture.Scope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        fixture.Revisions
            .Setup(repository => repository.GetAsync(fixture.Scope.Key, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(fixture.Scope.Key, 9, NowUtc));

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(8),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyAll();
        fixture.Builder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenSourceMutationIsPending_ShouldRetryWithoutReadingSources()
    {
        HandlerFixture fixture = new HandlerFixture();
        fixture.Snapshots
            .Setup(repository => repository.GetPointerAsync(fixture.Scope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        fixture.Revisions
            .Setup(repository => repository.GetAsync(fixture.Scope.Key, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                fixture.Scope.Key,
                8,
                NowUtc,
                PendingMutationCount: 1,
                MutationLeaseExpiresAtUtc: NowUtc.AddMinutes(30)));

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(8),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Retry, result.Outcome);
        Assert.Equal(RatingRankingRebuildErrorCodes.SourceRevisionUnavailable, result.ErrorCode);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyAll();
        fixture.Builder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenGlobalParkItemMutationIsPending_ShouldRetryCategoryWithoutReadingSources()
    {
        HandlerFixture fixture = new HandlerFixture();
        fixture.Snapshots
            .Setup(repository => repository.GetPointerAsync(fixture.Scope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        fixture.Revisions
            .Setup(repository => repository.GetAsync(fixture.Scope.Key, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(fixture.Scope.Key, 8, NowUtc));
        fixture.Revisions
            .Setup(repository => repository.GetAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                CanonicalRankingScopes.GlobalParks.Key,
                12,
                NowUtc,
                PendingMutationCount: 1,
                MutationLeaseExpiresAtUtc: NowUtc.AddMinutes(30)));

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(8),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Retry, result.Outcome);
        Assert.Equal(RatingRankingRebuildErrorCodes.SourceRevisionUnavailable, result.ErrorCode);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyAll();
        fixture.Builder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenSourceBatchIsTruncated_ShouldDeadLetterWithoutStartingBuild()
    {
        HandlerFixture fixture = new HandlerFixture();
        fixture.SetupUncoveredRevision(5);
        fixture.Builder
            .Setup(builder => builder.BuildAsync(fixture.Scope, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSnapshotBuildPlan(
                RankingSnapshotHeader.MaximumCandidateEntryCount,
                Array.Empty<RankingSnapshotEntry>(),
                true));
        fixture.Revisions
            .Setup(repository => repository.MarkUnavailableAsync(
                fixture.Scope.Key,
                fixture.Scope.MethodologyVersion,
                5,
                RatingRankingRebuildErrorCodes.SourceSetTruncated,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(5),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.DeadLetter, result.Outcome);
        Assert.Equal(RatingRankingRebuildErrorCodes.SourceSetTruncated, result.ErrorCode);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyAll();
        fixture.Builder.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenAChunkWriteConflicts_ShouldFailBuildAndNeverPublishPointer()
    {
        HandlerFixture fixture = new HandlerFixture();
        IReadOnlyCollection<RankingSnapshotEntry> entries = fixture.CreateEligibleEntries();
        fixture.SetupUncoveredRevision(5);
        fixture.Builder
            .Setup(builder => builder.BuildAsync(fixture.Scope, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSnapshotBuildPlan(entries.Count, entries, false));
        fixture.Snapshots
            .Setup(repository => repository.StartBuildAsync(
                It.Is<StartRankingSnapshotBuildRequest>(request =>
                    request.SourceRevision == 5
                    && request.EligibleEntryCount == entries.Count),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotBuildStartResult(
                RankingSnapshotBuildStartDisposition.Created,
                fixture.CreateHeader(RankingSnapshotStatus.Building)));
        fixture.Snapshots
            .Setup(repository => repository.WriteChunkAsync(
                It.Is<RankingSnapshotChunk>(chunk => chunk.Entries.Count == entries.Count),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotChunkWriteResult(
                RankingSnapshotChunkWriteDisposition.Conflict));
        fixture.Snapshots
            .Setup(repository => repository.FailBuildAsync(
                RankingSnapshotId.Parse("snapshot-1"),
                1,
                RatingRankingRebuildErrorCodes.ChunkWriteConflict,
                CancellationToken.None))
            .ReturnsAsync(true);

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(5),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Retry, result.Outcome);
        Assert.Equal(RatingRankingRebuildErrorCodes.ChunkWriteConflict, result.ErrorCode);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyAll();
        fixture.Builder.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenScopeFallsBelowThreshold_ShouldRetireWithoutStartingBuild()
    {
        HandlerFixture fixture = new HandlerFixture();
        IReadOnlyCollection<RankingSnapshotEntry> entries = fixture.CreateEligibleEntries()
            .Take(2)
            .ToArray();
        fixture.SetupUncoveredRevision(6);
        fixture.Builder
            .Setup(builder => builder.BuildAsync(fixture.Scope, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSnapshotBuildPlan(entries.Count, entries, false));
        fixture.Snapshots
            .Setup(repository => repository.RetirePublicationAsync(
                It.Is<RetireRankingPublicationRequest>(request =>
                    request.ScopeKey == fixture.Scope.Key
                    && request.MethodologyVersion == fixture.Scope.MethodologyVersion
                    && request.SourceRevision == 6),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotRetirementResult(
                RankingSnapshotRetirementDisposition.Retired,
                fixture.CreatePointer(5)));
        fixture.Revisions
            .Setup(repository => repository.MarkUnavailableAsync(
                fixture.Scope.Key,
                fixture.Scope.MethodologyVersion,
                6,
                RatingRankingRebuildErrorCodes.BelowMinimumEligibleEntries,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(6),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyAll();
        fixture.Builder.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenBuildIsCompleteAndStillCurrent_ShouldPublishValidatedSnapshot()
    {
        HandlerFixture fixture = new HandlerFixture();
        IReadOnlyCollection<RankingSnapshotEntry> entries = fixture.CreateEligibleEntries();
        fixture.SetupUncoveredRevision(6);
        fixture.Builder
            .Setup(builder => builder.BuildAsync(fixture.Scope, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSnapshotBuildPlan(entries.Count, entries, false));
        fixture.Snapshots
            .Setup(repository => repository.StartBuildAsync(
                It.IsAny<StartRankingSnapshotBuildRequest>(),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotBuildStartResult(
                RankingSnapshotBuildStartDisposition.Created,
                fixture.CreateHeader(RankingSnapshotStatus.Building, sourceRevision: 6)));
        fixture.Snapshots
            .Setup(repository => repository.WriteChunkAsync(
                It.Is<RankingSnapshotChunk>(chunk =>
                    chunk.SnapshotId == RankingSnapshotId.Parse("snapshot-1")
                    && chunk.BuildAttempt == 1
                    && chunk.Entries.Count == 3),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotChunkWriteResult(
                RankingSnapshotChunkWriteDisposition.Written));
        fixture.Snapshots
            .Setup(repository => repository.ValidateBuildAsync(
                RankingSnapshotId.Parse("snapshot-1"),
                1,
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotValidationResult(
                RankingSnapshotValidationDisposition.Validated,
                fixture.CreateHeader(RankingSnapshotStatus.Validated, sourceRevision: 6)));
        fixture.Snapshots
            .Setup(repository => repository.PublishAsync(
                RankingSnapshotId.Parse("snapshot-1"),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotPublicationResult(
                RankingSnapshotPublicationDisposition.Published,
                fixture.CreatePointer(6)));

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(6),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyAll();
        fixture.Builder.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenGlobalFenceChangesBeforePublication_ShouldRetryWithoutPublishing()
    {
        HandlerFixture fixture = new HandlerFixture();
        IReadOnlyCollection<RankingSnapshotEntry> entries = fixture.CreateEligibleEntries();
        fixture.SetupUncoveredRevision(6);
        fixture.Revisions
            .SetupSequence(repository => repository.GetAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                CanonicalRankingScopes.GlobalParks.Key,
                20,
                NowUtc))
            .ReturnsAsync(new RatingRankingSourceRevision(
                CanonicalRankingScopes.GlobalParks.Key,
                20,
                NowUtc))
            .ReturnsAsync(new RatingRankingSourceRevision(
                CanonicalRankingScopes.GlobalParks.Key,
                21,
                NowUtc));
        fixture.Builder
            .Setup(builder => builder.BuildAsync(fixture.Scope, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSnapshotBuildPlan(entries.Count, entries, false));
        fixture.Snapshots
            .Setup(repository => repository.StartBuildAsync(
                It.IsAny<StartRankingSnapshotBuildRequest>(),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotBuildStartResult(
                RankingSnapshotBuildStartDisposition.Created,
                fixture.CreateHeader(RankingSnapshotStatus.Building, sourceRevision: 6)));
        fixture.Snapshots
            .Setup(repository => repository.WriteChunkAsync(
                It.IsAny<RankingSnapshotChunk>(),
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotChunkWriteResult(
                RankingSnapshotChunkWriteDisposition.Written));
        fixture.Snapshots
            .Setup(repository => repository.ValidateBuildAsync(
                RankingSnapshotId.Parse("snapshot-1"),
                1,
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotValidationResult(
                RankingSnapshotValidationDisposition.Validated,
                fixture.CreateHeader(RankingSnapshotStatus.Validated, sourceRevision: 6)));

        DurableBackgroundJobHandlerResult result = await fixture.Handler.HandleAsync(
            fixture.CreateContext(6),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Retry, result.Outcome);
        Assert.Equal(RatingRankingRebuildErrorCodes.SourceRevisionUnavailable, result.ErrorCode);
        fixture.Snapshots.VerifyAll();
        fixture.Revisions.VerifyAll();
        fixture.Builder.VerifyAll();
    }

    private sealed class HandlerFixture
    {
        public HandlerFixture()
        {
            this.Scope = CanonicalRankingScopes.PublicItemCategories.Single(
                static scope => scope.Filter.ParkItemCategory == ParkItemCategory.Attraction);
            RankingScopeRegistry registry = new RankingScopeRegistry(
                "test-scopes",
                new[] { this.Scope });
            this.Revisions = new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
            this.Snapshots = new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
            this.Builder = new Mock<IRatingRankingSnapshotBuilder>(MockBehavior.Strict);
            this.Handler = new RatingRankingRebuildScopeJobHandler(
                registry,
                this.Revisions.Object,
                this.Snapshots.Object,
                this.Builder.Object,
                new RankingSnapshotChecksumCalculator());
        }

        public RankingScopeDefinition Scope { get; }

        public Mock<IRatingRankingSourceRevisionRepository> Revisions { get; }

        public Mock<IRankingSnapshotRepository> Snapshots { get; }

        public Mock<IRatingRankingSnapshotBuilder> Builder { get; }

        public RatingRankingRebuildScopeJobHandler Handler { get; }

        public DurableBackgroundJobExecutionContext CreateContext(
            long requestedRevision,
            bool forceRebuild = false)
        {
            RatingRankingRebuildScopePayload payload = new RatingRankingRebuildScopePayload(
                this.Scope.Key.Value,
                requestedRevision,
                this.Scope.MethodologyVersion.Value,
                forceRebuild);
            return new DurableBackgroundJobExecutionContext(
                "job-1",
                RatingRankingRebuildScopeJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload),
                requestedRevision,
                1,
                "correlation-1");
        }

        public void SetupUncoveredRevision(long requestedRevision)
        {
            this.Snapshots
                .Setup(repository => repository.GetPointerAsync(this.Scope.Key, CancellationToken.None))
                .ReturnsAsync((RankingPublicationPointer?)null);
            this.Revisions
                .Setup(repository => repository.GetAsync(this.Scope.Key, CancellationToken.None))
                .ReturnsAsync(new RatingRankingSourceRevision(this.Scope.Key, requestedRevision, NowUtc));
            this.Revisions
                .Setup(repository => repository.GetAsync(
                    CanonicalRankingScopes.GlobalParks.Key,
                    CancellationToken.None))
                .ReturnsAsync(new RatingRankingSourceRevision(
                    CanonicalRankingScopes.GlobalParks.Key,
                    20,
                    NowUtc));
        }

        public IReadOnlyCollection<RankingSnapshotEntry> CreateEligibleEntries()
        {
            RankingEvidence evidence = RankingEligibilityPolicy.Initial.EvaluateSimpleTarget(
                new SimpleRankingEvidenceInput(10, 10, true, false, true));
            return new[]
            {
                new RankingSnapshotEntry(1, 1, RatingTargetType.ParkItem, "item-1", ParkItemCategory.Attraction, 4.5, evidence),
                new RankingSnapshotEntry(2, 2, RatingTargetType.ParkItem, "item-2", ParkItemCategory.Attraction, 4.25, evidence),
                new RankingSnapshotEntry(3, 3, RatingTargetType.ParkItem, "item-3", ParkItemCategory.Attraction, 4.0, evidence),
            };
        }

        public RankingSnapshotHeader CreateHeader(
            RankingSnapshotStatus status,
            long sourceRevision = 5,
            int entryCount = 3)
        {
            DateTime? validatedAtUtc = status is RankingSnapshotStatus.Validated
                or RankingSnapshotStatus.Current
                or RankingSnapshotStatus.Superseded
                    ? NowUtc
                    : null;
            DateTime? publishedAtUtc = status is RankingSnapshotStatus.Current
                or RankingSnapshotStatus.Superseded
                    ? NowUtc
                    : null;
            return new RankingSnapshotHeader(
                RankingSnapshotId.Parse("snapshot-1"),
                this.Scope.Key,
                this.Scope.MethodologyVersion,
                sourceRevision,
                status,
                entryCount,
                entryCount,
                this.Scope.PageSize,
                1,
                PlaceholderChecksum,
                NowUtc,
                validatedAtUtc,
                publishedAtUtc);
        }

        public RankingPublicationPointer CreatePointer(long revision)
        {
            return new RankingPublicationPointer(
                this.Scope.Key,
                RankingSnapshotId.Parse("snapshot-1"),
                NowUtc,
                null,
                null,
                this.Scope.MethodologyVersion,
                revision,
                revision,
                1,
                NowUtc);
        }

        public void VerifyNoCalls()
        {
            this.Revisions.VerifyNoOtherCalls();
            this.Snapshots.VerifyNoOtherCalls();
            this.Builder.VerifyNoOtherCalls();
        }
    }
}
