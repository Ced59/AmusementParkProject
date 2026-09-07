using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class SharePublicationPublisherVisitSnapshotTests
{
    private const string ShareTokenValue =
        "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA";

    [Fact]
    public async Task PublishAsync_ShouldFreezeTheApprovedVisitBeforeMakingItsLinkResolvable()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Month,
            new[] { ShareContentField.RideCount });
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                "owner-1",
                SharePublicationType.VisitRecap,
                "scope-1",
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        repository.Setup(value => value.CreateAsync(
                It.Is<SharePublication>(publication =>
                    publication.Status == SharePublicationStatus.Draft
                    && publication.ContentFingerprint == "selection-a"),
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        bool snapshotWasWritten = false;
        repository.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(publication =>
                    publication.Status == SharePublicationStatus.Published
                    && publication.PublicationVersion == 1),
                0,
                CancellationToken.None))
            .Callback(() => Assert.True(snapshotWasWritten))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokens = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        tokens.Setup(value => value.Generate()).Returns(ShareToken.Parse(ShareTokenValue));
        Mock<ISharePublicationSourceDescriptor> source =
            new Mock<ISharePublicationSourceDescriptor>(MockBehavior.Strict);
        source.Setup(value => value.GetCurrentSourceVersionAsync(
                "scope-1",
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<long>.Success(12));
        Mock<ISharePublicationSnapshotWriter> snapshots =
            new Mock<ISharePublicationSnapshotWriter>(MockBehavior.Strict);
        snapshots.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.VisitRecap);
        snapshots.Setup(value => value.WriteAsync(
                It.Is<SharePublicationSnapshotWriteRequest>(request =>
                    request.PublicationVersion == 1
                    && request.PublicationStateVersion == 0
                    && request.SourceVersion == 12
                    && request.SourceId == "visit-1"
                    && request.ContentFingerprint == "selection-a"
                    && request.VisitRecap != null),
                CancellationToken.None))
            .Callback(() => snapshotWasWritten = true)
            .ReturnsAsync(ApplicationResult<bool>.Success(true));
        SharePublicationPublisher publisher = new SharePublicationPublisher(
            repository.Object,
            tokens.Object,
            snapshotWriters: new[] { snapshots.Object });

        ApplicationResult<SharePublicationSettingsResult> result = await publisher.PublishAsync(
            "owner-1",
            SharePublicationType.VisitRecap,
            "scope-1",
            12,
            new SharePublicationApprovalState(null, null),
            policy,
            source.Object,
            CancellationToken.None,
            "selection-a",
            new VisitRecapShareInput(new[] { "item-1" }, null),
            "visit-1");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsPublic);
        Assert.True(snapshotWasWritten);
        repository.VerifyAll();
        tokens.VerifyAll();
        source.Verify(value => value.GetCurrentSourceVersionAsync(
            "scope-1",
            CancellationToken.None), Times.Exactly(2));
        source.VerifyAll();
        snapshots.VerifyAll();
    }
}
