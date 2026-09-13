using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class SharePublicationLifecycleServiceTests
{
    private const string OwnerId = "owner-1";
    private const string PublicationId = "publication-1";
    private const string PreviousToken = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private const string RotatedToken = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHhA";
    private static readonly DateTime Now = new DateTime(2026, 9, 13, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RotateAsync_ShouldCloneTheApprovedSnapshotBeforeReplacingTheOpaqueLink()
    {
        SharePublication publication = CreatePublishedPublication(SharePublicationType.VisitRecap);
        Mock<ISharePublicationRepository> repository = CreateOwnedRepository(publication);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.PublicationVersion == 2
                    && candidate.Version == 2
                    && candidate.ShareToken == ShareToken.Parse(RotatedToken)
                    && candidate.IsResolvable),
                1,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        tokenFactory.Setup(value => value.Generate()).Returns(ShareToken.Parse(RotatedToken));
        ISharePublicationSourceDescriptor source = CreateSource(SharePublicationType.VisitRecap, 7);
        Mock<ISharePublicationSnapshotWriter> snapshotWriter =
            new Mock<ISharePublicationSnapshotWriter>(MockBehavior.Strict);
        snapshotWriter.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.VisitRecap);
        snapshotWriter.Setup(value => value.CloneAsync(
                It.Is<SharePublicationSnapshotCloneRequest>(request =>
                    request.PublicationId == publication.Id
                    && request.SourcePublicationVersion == 1
                    && request.TargetPublicationVersion == 2
                    && request.PublicationStateVersion == 1
                    && request.SourceVersion == 7),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<bool>.Success(true));
        Mock<ISharePublicationCacheInvalidationQueue> queue =
            new Mock<ISharePublicationCacheInvalidationQueue>(MockBehavior.Strict);
        queue.Setup(value => value.Enqueue(
            It.Is<SharePublicationCacheInvalidationRequest>(request =>
                request.PublicationId == PublicationId
                && request.PublicationType == SharePublicationType.VisitRecap
                && request.ShareIds.Contains(PreviousToken)
                && request.ShareIds.Contains(RotatedToken))));
        SharePublicationLifecycleService service = CreateService(
            repository.Object,
            tokenFactory.Object,
            new[] { source },
            new[] { snapshotWriter.Object },
            queue.Object);

        ApplicationResult<SharePublicationSettingsResult> result = await service.RotateAsync(
            OwnerId,
            PublicationId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RotatedToken, result.Value!.ShareId);
        Assert.Equal(2, result.Value.PublicationVersion);
        snapshotWriter.VerifyAll();
        queue.VerifyAll();
        repository.VerifyAll();
        tokenFactory.VerifyAll();
    }

    [Fact]
    public async Task RotateAsync_WhenSourceChanged_ShouldLeaveTheCurrentLinkUntouched()
    {
        SharePublication publication = CreatePublishedPublication(SharePublicationType.VisitRecap);
        Mock<ISharePublicationRepository> repository = CreateOwnedRepository(publication);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSource(SharePublicationType.VisitRecap, 8);
        SharePublicationLifecycleService service = CreateService(
            repository.Object,
            tokenFactory.Object,
            new[] { source },
            Array.Empty<ISharePublicationSnapshotWriter>(),
            null);

        ApplicationResult<SharePublicationSettingsResult> result = await service.RotateAsync(
            OwnerId,
            PublicationId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.preview-expired");
        Assert.Equal(PreviousToken, publication.ShareToken!.Value.Value);
        Assert.Equal(1, publication.PublicationVersion);
        repository.VerifyAll();
        tokenFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RevokeByIdAsync_ShouldRemainSuccessfulWhenDerivedCacheConvergenceIsUnavailable()
    {
        SharePublication publication = CreatePublishedPublication(SharePublicationType.PersonalRanking);
        Mock<ISharePublicationRepository> repository = CreateOwnedRepository(publication);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.Status == SharePublicationStatus.Revoked
                    && candidate.ShareToken == null
                    && candidate.PublicationVersion == 2),
                1,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<ISharePublicationCacheInvalidationQueue> queue =
            new Mock<ISharePublicationCacheInvalidationQueue>(MockBehavior.Strict);
        queue.Setup(value => value.Enqueue(It.IsAny<SharePublicationCacheInvalidationRequest>()))
            .Throws(new InvalidOperationException("cache unavailable"));
        SharePublicationLifecycleService service = CreateService(
            repository.Object,
            Mock.Of<IShareTokenFactory>(MockBehavior.Strict),
            new[] { CreateSource(SharePublicationType.PersonalRanking, 7) },
            Array.Empty<ISharePublicationSnapshotWriter>(),
            queue.Object);

        ApplicationResult<SharePublicationSettingsResult> result = await service.RevokeByIdAsync(
            OwnerId,
            PublicationId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsPublic);
        Assert.Null(result.Value.ShareId);
        Assert.Equal(2, result.Value.PublicationVersion);
        repository.VerifyAll();
        queue.VerifyAll();
    }

    [Fact]
    public async Task RevokeByIdAsync_WhenPublicationDoesNotBelongToTheUser_ShouldReturnNotFound()
    {
        Mock<ISharePublicationRepository> repository = new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                SharePublicationId.Parse(PublicationId),
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        SharePublicationLifecycleService service = CreateService(
            repository.Object,
            Mock.Of<IShareTokenFactory>(MockBehavior.Strict),
            Array.Empty<ISharePublicationSourceDescriptor>(),
            Array.Empty<ISharePublicationSnapshotWriter>(),
            null);

        ApplicationResult<SharePublicationSettingsResult> result = await service.RevokeByIdAsync(
            OwnerId,
            PublicationId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.not-found");
        repository.VerifyAll();
    }

    private static Mock<ISharePublicationRepository> CreateOwnedRepository(
        SharePublication publication)
    {
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                publication.Id,
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync(publication);
        return repository;
    }

    private static ISharePublicationSourceDescriptor CreateSource(
        SharePublicationType publicationType,
        long sourceVersion)
    {
        Mock<ISharePublicationSourceDescriptor> source =
            new Mock<ISharePublicationSourceDescriptor>(MockBehavior.Strict);
        source.SetupGet(value => value.PublicationType).Returns(publicationType);
        source.Setup(value => value.GetCurrentSourceVersionAsync(
                It.Is<SharePublicationSourceVersionRequest>(request =>
                    request.SourceScopeKey == $"scope:{OwnerId}"
                    && request.PublicationId == SharePublicationId.Parse(PublicationId)),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<long>.Success(sourceVersion));
        return source.Object;
    }

    private static SharePublicationLifecycleService CreateService(
        ISharePublicationRepository repository,
        IShareTokenFactory tokenFactory,
        IEnumerable<ISharePublicationSourceDescriptor> sources,
        IEnumerable<ISharePublicationSnapshotWriter> snapshotWriters,
        ISharePublicationCacheInvalidationQueue? queue)
    {
        return new SharePublicationLifecycleService(
            repository,
            tokenFactory,
            sources,
            snapshotWriters,
            queue,
            new SharePublicationFixedTimeProvider(Now));
    }

    private static SharePublication CreatePublishedPublication(SharePublicationType publicationType)
    {
        ShareContentPolicy policy = ShareContentPolicy.CreatePrivateDefault(publicationType);
        return SharePublication.Restore(
            SharePublicationId.Parse(PublicationId),
            OwnerId,
            publicationType,
            $"scope:{OwnerId}",
            ShareToken.Parse(PreviousToken),
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            policy,
            7,
            1,
            1,
            Now.AddDays(-1),
            null,
            Now.AddDays(-2),
            Now.AddDays(-1));
    }
}
