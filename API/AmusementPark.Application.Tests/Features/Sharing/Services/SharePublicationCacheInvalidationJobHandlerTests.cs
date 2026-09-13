using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class SharePublicationCacheInvalidationJobHandlerTests
{
    private const string OwnerId = "owner-1";
    private const string ShareId = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_WhenPublicationMutationIsNotCommitted_ShouldRetryWithoutPurging()
    {
        SharePublication publication = CreatePublication(version: 1);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                publication.Id,
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<ISharePublicationCacheInvalidationExecutor> executor =
            new Mock<ISharePublicationCacheInvalidationExecutor>(MockBehavior.Strict);
        SharePublicationCacheInvalidationJobHandler handler =
            CreateHandler(repository.Object, executor.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(publication, minimumVersion: 2),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Retry, result.Outcome);
        Assert.Equal("sharing-cache-invalidation.state-not-committed", result.ErrorCode);
        repository.VerifyAll();
        executor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicationMutationIsCommitted_ShouldPurgeEveryShareId()
    {
        SharePublication publication = CreatePublication(version: 2);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                publication.Id,
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<ISharePublicationCacheInvalidationExecutor> executor =
            new Mock<ISharePublicationCacheInvalidationExecutor>(MockBehavior.Strict);
        executor.Setup(value => value.TryInvalidateAsync(
                It.Is<SharePublicationCacheInvalidationRequest>(request =>
                    request.PublicationId == publication.Id.Value
                    && request.PublicationType == SharePublicationType.VisitRecap
                    && request.ShareIds.SequenceEqual(new[] { ShareId })),
                CancellationToken.None))
            .ReturnsAsync(true);
        SharePublicationCacheInvalidationJobHandler handler =
            CreateHandler(repository.Object, executor.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(publication, minimumVersion: 2),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        repository.VerifyAll();
        executor.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicationWasRemoved_ShouldStillPurgeTheRecordedShareId()
    {
        SharePublication publication = CreatePublication(version: 2);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                publication.Id,
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        Mock<ISharePublicationCacheInvalidationExecutor> executor =
            new Mock<ISharePublicationCacheInvalidationExecutor>(MockBehavior.Strict);
        executor.Setup(value => value.TryInvalidateAsync(
                It.IsAny<SharePublicationCacheInvalidationRequest>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        SharePublicationCacheInvalidationJobHandler handler =
            CreateHandler(repository.Object, executor.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(publication, minimumVersion: 2),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        repository.VerifyAll();
        executor.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSsrOutagePersists_ShouldContinueInANewDurableJob()
    {
        SharePublication publication = CreatePublication(version: 2);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                publication.Id,
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<ISharePublicationCacheInvalidationExecutor> executor =
            new Mock<ISharePublicationCacheInvalidationExecutor>(MockBehavior.Strict);
        executor.Setup(value => value.TryInvalidateAsync(
                It.IsAny<SharePublicationCacheInvalidationRequest>(),
                CancellationToken.None))
            .ReturnsAsync(false);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.IdempotencyKey.EndsWith(":continuation:1", StringComparison.Ordinal)
                    && request.Payload
                        .Deserialize<SharePublicationCacheInvalidationJobPayload>()!
                        .Continuation == 1),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        SharePublicationCacheInvalidationJobHandler handler =
            CreateHandler(repository.Object, executor.Object, jobs.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(publication, minimumVersion: 2, attemptCount: 50),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        repository.VerifyAll();
        executor.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicationReadOutagePersists_ShouldContinueInANewDurableJob()
    {
        SharePublication publication = CreatePublication(version: 2);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                publication.Id,
                OwnerId,
                CancellationToken.None))
            .ThrowsAsync(new TimeoutException("MongoDB unavailable."));
        Mock<ISharePublicationCacheInvalidationExecutor> executor =
            new Mock<ISharePublicationCacheInvalidationExecutor>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.IdempotencyKey.EndsWith(":continuation:1", StringComparison.Ordinal)),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        SharePublicationCacheInvalidationJobHandler handler =
            CreateHandler(repository.Object, executor.Object, jobs.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(publication, minimumVersion: 2, attemptCount: 50),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        repository.VerifyAll();
        executor.VerifyNoOtherCalls();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_AfterRotation_ShouldDeleteOnlySnapshotsOlderThanRotatedVersion()
    {
        SharePublication publication = CreatePublication(version: 2);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                publication.Id,
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<ISharePublicationCacheInvalidationExecutor> executor =
            new Mock<ISharePublicationCacheInvalidationExecutor>(MockBehavior.Strict);
        executor.Setup(value => value.TryInvalidateAsync(
                It.IsAny<SharePublicationCacheInvalidationRequest>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<ISharePublicationSnapshotWriter> snapshots =
            new Mock<ISharePublicationSnapshotWriter>(MockBehavior.Strict);
        snapshots.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.VisitRecap);
        snapshots.Setup(value => value.DeleteSupersededAsync(
                publication.Id,
                2,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<bool>.Success(true));
        SharePublicationCacheInvalidationJobHandler handler = CreateHandler(
            repository.Object,
            executor.Object,
            snapshotWriters: new[] { snapshots.Object });

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(
                publication,
                minimumVersion: 2,
                snapshotCleanupPublicationVersion: 2),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        repository.VerifyAll();
        executor.VerifyAll();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenRotationCleanupPersists_ShouldContinueWithTheCleanupVersion()
    {
        SharePublication publication = CreatePublication(version: 2);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                publication.Id,
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<ISharePublicationCacheInvalidationExecutor> executor =
            new Mock<ISharePublicationCacheInvalidationExecutor>(MockBehavior.Strict);
        executor.Setup(value => value.TryInvalidateAsync(
                It.IsAny<SharePublicationCacheInvalidationRequest>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<ISharePublicationSnapshotWriter> snapshots =
            new Mock<ISharePublicationSnapshotWriter>(MockBehavior.Strict);
        snapshots.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.VisitRecap);
        snapshots.Setup(value => value.DeleteSupersededAsync(
                publication.Id,
                2,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<bool>.Failure(
                ApplicationError.Technical("snapshot.unavailable", "Snapshot unavailable.")));
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Payload
                        .Deserialize<SharePublicationCacheInvalidationJobPayload>()!
                        .SnapshotCleanupPublicationVersion == 2
                    && request.Payload
                        .Deserialize<SharePublicationCacheInvalidationJobPayload>()!
                        .Continuation == 1),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        SharePublicationCacheInvalidationJobHandler handler = CreateHandler(
            repository.Object,
            executor.Object,
            jobs.Object,
            new[] { snapshots.Object });

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(
                publication,
                minimumVersion: 2,
                attemptCount: 50,
                snapshotCleanupPublicationVersion: 2),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        repository.VerifyAll();
        executor.VerifyAll();
        snapshots.VerifyAll();
        jobs.VerifyAll();
    }

    private static DurableBackgroundJobExecutionContext CreateContext(
        SharePublication publication,
        long minimumVersion,
        int attemptCount = 1,
        long? snapshotCleanupPublicationVersion = null)
    {
        SharePublicationCacheInvalidationJobPayload payload =
            new SharePublicationCacheInvalidationJobPayload(
                publication.Id.Value,
                OwnerId,
                publication.Type,
                minimumVersion,
                new[] { ShareId },
                SnapshotCleanupPublicationVersion: snapshotCleanupPublicationVersion);
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            SharePublicationCacheInvalidationJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            null,
            attemptCount,
            null);
    }

    private static SharePublicationCacheInvalidationJobHandler CreateHandler(
        ISharePublicationRepository repository,
        ISharePublicationCacheInvalidationExecutor executor,
        IDurableBackgroundJobRepository? jobs = null,
        IEnumerable<ISharePublicationSnapshotWriter>? snapshotWriters = null)
    {
        IDurableBackgroundJobRepository jobRepository = jobs
            ?? new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict).Object;
        return new SharePublicationCacheInvalidationJobHandler(
            repository,
            executor,
            new SharePublicationCacheInvalidationScheduler(jobRepository),
            snapshotWriters ?? Array.Empty<ISharePublicationSnapshotWriter>());
    }

    private static SharePublication CreatePublication(long version)
    {
        return SharePublication.Restore(
            SharePublicationId.New(),
            OwnerId,
            SharePublicationType.VisitRecap,
            VisitRecapShareSourceScope.Create(OwnerId, "visit-1"),
            ShareToken.Parse(ShareId),
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            ShareContentPolicy.CreatePrivateDefault(SharePublicationType.VisitRecap),
            1,
            1,
            version,
            NowUtc,
            null,
            NowUtc,
            NowUtc);
    }
}
