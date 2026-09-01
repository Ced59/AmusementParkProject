using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
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

        await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

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
        jobs.VerifyAll();
        revisions.VerifyNoOtherCalls();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task ScheduleIfOutstandingAsync_WhenPublishedPointerCoversRevision_ShouldSkipJob()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(scope.Key, 12, NowUtc);
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

        await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

        jobs.VerifyNoOtherCalls();
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

        await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

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

        await scheduler.ScheduleIfOutstandingAsync(revision, CancellationToken.None);

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
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(scope.Key, 8, NowUtc);
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

    private static RatingRankingRebuildScheduler CreateScheduler(
        RankingScopeDefinition scope,
        IDurableBackgroundJobRepository jobs,
        IRatingRankingSourceRevisionRepository revisions,
        IRankingSnapshotRepository snapshots)
    {
        RankingScopeRegistry registry = new RankingScopeRegistry("test-scopes", new[] { scope });
        return new RatingRankingRebuildScheduler(jobs, revisions, snapshots, registry);
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
