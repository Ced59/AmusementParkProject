using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class YearRecapShareSnapshotWriterTests
{
    private static readonly DateTime Now = new DateTime(2026, 9, 13, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task WriteAsync_WhenTheYearIsEmpty_ShouldRefuseToCreateAPublicSnapshot()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.YearRecap,
            ShareDatePrecision.Year,
            Array.Empty<ShareContentField>());
        YearRecapSharePreviewResult emptyRecap = new YearRecapSharePreviewResult(
            2026, null, 0, 0, 0, null, null, null,
            Array.Empty<string>(), null, null,
            Array.Empty<YearRecapShareParkResult>(), null, null, null,
            Array.Empty<YearRecapShareHighlightResult>(), null, false,
            "passport-year-recap-v1", true);
        Mock<IYearRecapSharePreviewBuilder> builder =
            new Mock<IYearRecapSharePreviewBuilder>(MockBehavior.Strict);
        builder.Setup(value => value.BuildAsync(
                "owner-1",
                2026,
                policy,
                It.IsAny<YearRecapShareInput?>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharePublicationPreviewResult>.Success(
                new SharePublicationPreviewResult(
                    SharePublicationType.YearRecap,
                    3,
                    policy.SchemaVersion,
                    policy.DatePrecision,
                    policy.IncludedFields,
                    null,
                    YearRecap: emptyRecap,
                    ContentFingerprint: "fingerprint")));
        Mock<IYearRecapShareSnapshotRepository> snapshots =
            new Mock<IYearRecapShareSnapshotRepository>(MockBehavior.Strict);
        YearRecapShareSnapshotWriter writer = new YearRecapShareSnapshotWriter(
            builder.Object,
            snapshots.Object);

        ApplicationResult<bool> result = await writer.WriteAsync(
            new SharePublicationSnapshotWriteRequest(
                SharePublicationId.Parse("publication-1"),
                1,
                1,
                "owner-1",
                "2026",
                3,
                policy,
                "fingerprint",
                YearRecap: new YearRecapShareInput(null)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.year-recap-empty");
        snapshots.Verify(value => value.UpsertAsync(
            It.IsAny<YearRecapShareSnapshot>(),
            It.IsAny<CancellationToken>()), Times.Never);
        builder.VerifyAll();
    }

    [Fact]
    public async Task CloneAsync_ShouldCopyTheExactApprovedContentIntoTheNextPublicationVersion()
    {
        SharePublicationId publicationId = SharePublicationId.Parse("publication-1");
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.YearRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.RideCount, ShareContentField.PublicCaption });
        YearRecapSharePreviewResult content = new YearRecapSharePreviewResult(
            2026, 1, 1, 0, 0, 4, 3, null,
            Array.Empty<string>(), null, null,
            Array.Empty<YearRecapShareParkResult>(), null, null, null,
            Array.Empty<YearRecapShareHighlightResult>(), "Une belle année", false,
            "passport-year-recap-v1", false);
        YearRecapShareSnapshot current = new YearRecapShareSnapshot(
            publicationId,
            4,
            8,
            12,
            policy.SchemaVersion,
            policy.DatePrecision,
            policy.IncludedFields,
            "fingerprint",
            content,
            Now.AddDays(-1));
        Mock<IYearRecapShareSnapshotRepository> snapshots =
            new Mock<IYearRecapShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.GetAsync(publicationId, 4, CancellationToken.None))
            .ReturnsAsync(current);
        snapshots.Setup(value => value.UpsertAsync(
                It.Is<YearRecapShareSnapshot>(snapshot =>
                    snapshot.PublicationVersion == 5
                    && snapshot.PublicationStateVersion == 8
                    && ReferenceEquals(snapshot.Content, content)
                    && snapshot.CreatedAtUtc == Now),
                CancellationToken.None))
            .ReturnsAsync(true);
        YearRecapShareSnapshotWriter writer = new YearRecapShareSnapshotWriter(
            Mock.Of<IYearRecapSharePreviewBuilder>(MockBehavior.Strict),
            snapshots.Object,
            new SharePublicationFixedTimeProvider(Now));

        ApplicationResult<bool> result = await writer.CloneAsync(
            new SharePublicationSnapshotCloneRequest(
                publicationId,
                4,
                5,
                8,
                12,
                policy,
                "fingerprint"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        snapshots.VerifyAll();
    }
}
