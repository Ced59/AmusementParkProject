using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class SharePublicationPublisherYearSnapshotTests
{
    private const string ShareTokenValue =
        "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA";

    [Fact]
    public async Task PublishAsync_ShouldFreezeTheApprovedYearBeforeMakingItsLinkResolvable()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.YearRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.PublicCaption });
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                "owner-1",
                SharePublicationType.YearRecap,
                "year-scope",
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        repository.Setup(value => value.CreateAsync(
                It.Is<SharePublication>(publication =>
                    publication.Status == SharePublicationStatus.Draft
                    && publication.ContentFingerprint == "caption-fingerprint"),
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
                "year-scope",
                It.IsAny<ShareContentPolicy>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<long>.Success(12));
        Mock<ISharePublicationSnapshotWriter> snapshots =
            new Mock<ISharePublicationSnapshotWriter>(MockBehavior.Strict);
        snapshots.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.YearRecap);
        snapshots.Setup(value => value.WriteAsync(
                It.Is<SharePublicationSnapshotWriteRequest>(request =>
                    request.PublicationVersion == 1
                    && request.PublicationStateVersion == 0
                    && request.SourceVersion == 12
                    && request.SourceId == "2026"
                    && request.ContentFingerprint == "caption-fingerprint"
                    && request.VisitRecap == null
                    && request.YearRecap != null
                    && request.YearRecap.PublicCaption == "Mon année"),
                CancellationToken.None))
            .Callback(() => snapshotWasWritten = true)
            .ReturnsAsync(ApplicationResult<bool>.Success(true));
        snapshots.Setup(value => value.DeleteSupersededAsync(
                It.IsAny<SharePublicationId>(),
                1,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<bool>.Success(true));
        SharePublicationPublisher publisher = new SharePublicationPublisher(
            repository.Object,
            tokens.Object,
            snapshotWriters: new[] { snapshots.Object });

        ApplicationResult<SharePublicationSettingsResult> result = await publisher.PublishAsync(
            "owner-1",
            SharePublicationType.YearRecap,
            "year-scope",
            12,
            new SharePublicationApprovalState(null, null),
            policy,
            source.Object,
            CancellationToken.None,
            "caption-fingerprint",
            null,
            "2026",
            new YearRecapShareInput("Mon année"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsPublic);
        Assert.True(snapshotWasWritten);
        repository.VerifyAll();
        tokens.VerifyAll();
        source.Verify(value => value.GetCurrentSourceVersionAsync(
            "year-scope",
            It.IsAny<ShareContentPolicy>(),
            CancellationToken.None), Times.Exactly(2));
        source.VerifyAll();
        snapshots.VerifyAll();
    }
}
