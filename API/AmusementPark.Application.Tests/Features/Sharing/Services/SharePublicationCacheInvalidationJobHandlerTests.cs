using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
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
            new SharePublicationCacheInvalidationJobHandler(repository.Object, executor.Object);

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
            new SharePublicationCacheInvalidationJobHandler(repository.Object, executor.Object);

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
            new SharePublicationCacheInvalidationJobHandler(repository.Object, executor.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(publication, minimumVersion: 2),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        repository.VerifyAll();
        executor.VerifyAll();
    }

    private static DurableBackgroundJobExecutionContext CreateContext(
        SharePublication publication,
        long minimumVersion)
    {
        SharePublicationCacheInvalidationJobPayload payload =
            new SharePublicationCacheInvalidationJobPayload(
                publication.Id.Value,
                OwnerId,
                publication.Type,
                minimumVersion,
                new[] { ShareId });
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            SharePublicationCacheInvalidationJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            null,
            1,
            null);
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
