using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingRebuildSchedulerTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ScheduleIfOutstandingAsync_ShouldCoalesceLatestRevisionWithStableNaturalKey()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(scope.Key, 12, NowUtc);
        CoalesceBackgroundJobRequest? capturedRequest = null;
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.HasDeadLetteredRevisionAsync(
                RatingRankingRebuildScopeJob.Kind,
                "ratings.rebuild-scope:parks:global",
                12,
                RatingRankingRebuildScopeJob.PayloadVersion,
                It.IsAny<JsonElement>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.IsAny<CoalesceBackgroundJobRequest>(),
                CancellationToken.None))
            .Callback((CoalesceBackgroundJobRequest request, CancellationToken _) =>
                capturedRequest = request)
            .ReturnsAsync((CoalesceBackgroundJobRequest request, CancellationToken _) => CreateJob(request));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots
            .Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        RatingRankingRebuildScheduleDisposition disposition =
            await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

        Assert.Equal(RatingRankingRebuildScheduleDisposition.Scheduled, disposition);
        Assert.NotNull(capturedRequest);
        Assert.Equal(RatingRankingRebuildScopeJob.Kind, capturedRequest.Kind);
        Assert.Equal("ratings.rebuild-scope:parks:global", capturedRequest.NaturalKey);
        Assert.Equal(12, capturedRequest.RequestedRevision);
        Assert.Equal(RatingRankingRebuildScopeJob.PayloadVersion, capturedRequest.PayloadVersion);
        RatingRankingRebuildScopePayload? payload =
            capturedRequest.Payload.Deserialize<RatingRankingRebuildScopePayload>();
        Assert.NotNull(payload);
        Assert.Equal(scope.Key.Value, payload.ScopeKey);
        Assert.Equal(12, payload.RequestedSourceRevision);
        Assert.Equal(scope.MethodologyVersion.Value, payload.MethodologyVersion);
        Assert.False(payload.ForceRebuild);
        Assert.False(capturedRequest.Payload.TryGetProperty("forceRebuild", out JsonElement _));
        jobs.VerifyAll();
        revisions.VerifyNoOtherCalls();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task ScheduleForcedAsync_WhenPublishedPointerCoversRevision_ShouldStillEnqueueForcedJob()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(scope.Key, 12, NowUtc);
        CoalesceBackgroundJobRequest? capturedRequest = null;
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.IsAny<CoalesceBackgroundJobRequest>(),
                CancellationToken.None))
            .Callback((CoalesceBackgroundJobRequest request, CancellationToken _) =>
                capturedRequest = request)
            .ReturnsAsync((CoalesceBackgroundJobRequest request, CancellationToken _) => CreateJob(request));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        RatingRankingRebuildScheduleDisposition disposition =
            await scheduler.ScheduleForcedAsync(revision, CancellationToken.None);

        Assert.Equal(RatingRankingRebuildScheduleDisposition.Scheduled, disposition);
        Assert.NotNull(capturedRequest);
        Assert.Equal(
            RatingRankingRebuildScopeJob.BuildForcedNaturalKey(scope.Key),
            capturedRequest.NaturalKey);
        RatingRankingRebuildScopePayload? payload =
            capturedRequest.Payload.Deserialize<RatingRankingRebuildScopePayload>();
        Assert.NotNull(payload);
        Assert.Equal(12, payload.RequestedSourceRevision);
        Assert.True(payload.ForceRebuild);
        Assert.True(capturedRequest.Payload.TryGetProperty("forceRebuild", out JsonElement forceRebuild));
        Assert.True(forceRebuild.GetBoolean());
        jobs.VerifyAll();
        revisions.VerifyNoOtherCalls();
        snapshots.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ScheduleIfOutstandingAsync_WhenPublishedPointerCoversRevision_ShouldSkipJob()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(
            scope.Key,
            12,
            NowUtc,
            CacheConvergedMethodologyVersion: scope.MethodologyVersion,
            HighestCacheConvergedSourceRevision: 12);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots
            .Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(CreatePointer(scope, 12));
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        RatingRankingRebuildScheduleDisposition disposition =
            await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

        Assert.Equal(RatingRankingRebuildScheduleDisposition.Covered, disposition);
        jobs.VerifyNoOtherCalls();
        revisions.VerifyNoOtherCalls();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task ScheduleIfOutstandingAsync_WhenPublishedPointerCoversButCacheDidNotConverge_ShouldRequeueAfterDeadLetter()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(scope.Key, 12, NowUtc);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.NaturalKey == RatingRankingRebuildScopeJob.BuildNaturalKey(scope.Key)
                    && request.RequestedRevision == 12),
                CancellationToken.None))
            .ReturnsAsync((CoalesceBackgroundJobRequest request, CancellationToken _) => CreateJob(request));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots
            .Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(CreatePointer(scope, 12));
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        RatingRankingRebuildScheduleDisposition disposition =
            await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

        Assert.Equal(RatingRankingRebuildScheduleDisposition.Scheduled, disposition);
        jobs.VerifyAll();
        jobs.Verify(repository => repository.HasDeadLetteredRevisionAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<JsonElement>(),
            It.IsAny<CancellationToken>()), Times.Never);
        revisions.VerifyNoOtherCalls();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task ScheduleIfOutstandingAsync_WhenFinalLeaseExposesNewerRevision_ShouldEnqueueIt()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(scope.Key, 13, NowUtc);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.HasDeadLetteredRevisionAsync(
                RatingRankingRebuildScopeJob.Kind,
                "ratings.rebuild-scope:parks:global",
                13,
                RatingRankingRebuildScopeJob.PayloadVersion,
                It.IsAny<JsonElement>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request => request.RequestedRevision == 13),
                CancellationToken.None))
            .ReturnsAsync((CoalesceBackgroundJobRequest request, CancellationToken _) => CreateJob(request));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots
            .Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(CreatePointer(scope, 12));
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        RatingRankingRebuildScheduleDisposition disposition =
            await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

        Assert.Equal(RatingRankingRebuildScheduleDisposition.Scheduled, disposition);
        jobs.VerifyAll();
        revisions.VerifyNoOtherCalls();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task ScheduleIfOutstandingAsync_WhenUnavailableMarkerCoversRevision_ShouldSkipJobWithoutSnapshotRead()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(
            scope.Key,
            12,
            NowUtc,
            UnavailableMethodologyVersion: scope.MethodologyVersion,
            HighestUnavailableSourceRevision: 12,
            UnavailableReasonCode: RatingRankingRebuildErrorCodes.SourceSetTruncated);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        RatingRankingRebuildScheduleDisposition disposition =
            await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

        Assert.Equal(RatingRankingRebuildScheduleDisposition.Covered, disposition);
        jobs.VerifyNoOtherCalls();
        revisions.VerifyNoOtherCalls();
        snapshots.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ScheduleIfOutstandingAsync_WhenRecoveryEventIsPending_ShouldSkipJobWithoutSnapshotRead()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(
            scope.Key,
            12,
            NowUtc,
            RecoveredMutations: new[]
            {
                new RatingRankingRecoveredMutation(
                    1.ToString("x32"),
                    RatingTargetType.Park,
                    "park-1",
                    "user-1",
                    2.ToString("x32")),
            });
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        RatingRankingRebuildScheduleDisposition disposition =
            await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

        Assert.Equal(RatingRankingRebuildScheduleDisposition.Deferred, disposition);
        jobs.VerifyNoOtherCalls();
        revisions.VerifyNoOtherCalls();
        snapshots.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ScheduleOutstandingAsync_WhenNoRevisionOrPointerExists_ShouldScheduleInitialRevisionZero()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.HasDeadLetteredRevisionAsync(
                RatingRankingRebuildScopeJob.Kind,
                "ratings.rebuild-scope:parks:global",
                0,
                RatingRankingRebuildScopeJob.PayloadVersion,
                It.IsAny<JsonElement>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.RequestedRevision == 0
                    && request.NaturalKey == "ratings.rebuild-scope:parks:global"),
                CancellationToken.None))
            .ReturnsAsync((CoalesceBackgroundJobRequest request, CancellationToken _) => CreateJob(request));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync((RatingRankingSourceRevision?)null);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots
            .Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        await scheduler.ScheduleOutstandingAsync(CancellationToken.None);

        jobs.VerifyAll();
        revisions.VerifyAll();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task ScheduleOutstandingAsync_WhenPointerCoversCurrentMethodologyAndRevision_ShouldSkipJob()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(
            scope.Key,
            8,
            NowUtc,
            CacheConvergedMethodologyVersion: scope.MethodologyVersion,
            HighestCacheConvergedSourceRevision: 8);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(revision);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots
            .Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(CreatePointer(scope, 8));
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        await scheduler.ScheduleOutstandingAsync(CancellationToken.None);

        jobs.VerifyNoOtherCalls();
        revisions.VerifyAll();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task ScheduleOutstandingAsync_WhenMutationLeaseIsPending_ShouldNotExposeRevision()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(
            scope.Key,
            8,
            NowUtc,
            PendingMutationCount: 1,
            MutationLeaseExpiresAtUtc: NowUtc.AddMinutes(30));
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(revision);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        await scheduler.ScheduleOutstandingAsync(CancellationToken.None);

        jobs.VerifyNoOtherCalls();
        revisions.VerifyAll();
        snapshots.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ScheduleOutstandingAsync_WhenOverflowMarkerCoversRevision_ShouldSkipJob()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(
            scope.Key,
            8,
            NowUtc,
            UnavailableMethodologyVersion: scope.MethodologyVersion,
            HighestUnavailableSourceRevision: 8,
            UnavailableReasonCode: RatingRankingRebuildErrorCodes.SourceSetTruncated);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(revision);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        await scheduler.ScheduleOutstandingAsync(CancellationToken.None);

        jobs.VerifyNoOtherCalls();
        revisions.VerifyAll();
        snapshots.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ScheduleIfOutstandingAsync_WhenRevisionWasDeadLettered_ShouldNotRequeueIt()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(scope.Key, 12, NowUtc);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.HasDeadLetteredRevisionAsync(
                RatingRankingRebuildScopeJob.Kind,
                "ratings.rebuild-scope:parks:global",
                12,
                RatingRankingRebuildScopeJob.PayloadVersion,
                It.Is<JsonElement>(payload => payload.GetRawText().Contains(
                    scope.MethodologyVersion.Value,
                    StringComparison.Ordinal)),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots
            .Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        RatingRankingRebuildScheduler scheduler = CreateScheduler(
            scope,
            jobs.Object,
            revisions.Object,
            snapshots.Object);

        RatingRankingRebuildScheduleDisposition disposition =
            await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

        Assert.Equal(RatingRankingRebuildScheduleDisposition.Covered, disposition);
        jobs.VerifyAll();
        revisions.VerifyNoOtherCalls();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task ScheduleOutstandingAsync_WhenOneScopeFails_ShouldContinueWithRemainingScopes()
    {
        RankingScopeDefinition failingScope = CanonicalRankingScopes.GlobalParks;
        RankingScopeDefinition healthyScope = CanonicalRankingScopes.PublicItemCategories[0];
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.HasDeadLetteredRevisionAsync(
                RatingRankingRebuildScopeJob.Kind,
                RatingRankingRebuildScopeJob.BuildNaturalKey(healthyScope.Key),
                0,
                RatingRankingRebuildScopeJob.PayloadVersion,
                It.IsAny<JsonElement>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.NaturalKey == RatingRankingRebuildScopeJob.BuildNaturalKey(healthyScope.Key)
                    && request.RequestedRevision == 0),
                CancellationToken.None))
            .ReturnsAsync((CoalesceBackgroundJobRequest request, CancellationToken _) => CreateJob(request));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.GetAsync(failingScope.Key, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Invalid persisted revision"));
        revisions
            .Setup(repository => repository.GetAsync(healthyScope.Key, CancellationToken.None))
            .ReturnsAsync((RatingRankingSourceRevision?)null);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots
            .Setup(repository => repository.GetPointerAsync(healthyScope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        RankingScopeRegistry registry = new RankingScopeRegistry(
            "test-scopes",
            new[] { failingScope, healthyScope });
        RatingRankingRebuildScheduler scheduler = new RatingRankingRebuildScheduler(
            jobs.Object,
            revisions.Object,
            snapshots.Object,
            registry,
            NullLogger<RatingRankingRebuildScheduler>.Instance);

        await scheduler.ScheduleOutstandingAsync(CancellationToken.None);

        jobs.VerifyAll();
        revisions.VerifyAll();
        snapshots.VerifyAll();
    }

    private static RatingRankingRebuildScheduler CreateScheduler(
        RankingScopeDefinition scope,
        IDurableBackgroundJobRepository jobs,
        IRatingRankingSourceRevisionRepository revisions,
        IRankingSnapshotRepository snapshots)
    {
        RankingScopeRegistry registry = new RankingScopeRegistry("test-scopes", new[] { scope });
        return new RatingRankingRebuildScheduler(
            jobs,
            revisions,
            snapshots,
            registry,
            NullLogger<RatingRankingRebuildScheduler>.Instance);
    }

    private static DurableBackgroundJob CreateJob(CoalesceBackgroundJobRequest request)
    {
        return new DurableBackgroundJob(
            "job-1",
            request.Kind,
            request.NaturalKey,
            null,
            request.PayloadVersion,
            request.Payload,
            request.RequestedRevision,
            null,
            DurableBackgroundJobStatus.Pending,
            request.Priority,
            0,
            NowUtc,
            null,
            null,
            null,
            NowUtc,
            NowUtc,
            null,
            null,
            null);
    }

    private static RankingPublicationPointer CreatePointer(
        RankingScopeDefinition scope,
        long revision)
    {
        return new RankingPublicationPointer(
            scope.Key,
            RankingSnapshotId.Parse("snapshot-current"),
            NowUtc,
            null,
            null,
            scope.MethodologyVersion,
            revision,
            revision,
            1,
            NowUtc);
    }
}
