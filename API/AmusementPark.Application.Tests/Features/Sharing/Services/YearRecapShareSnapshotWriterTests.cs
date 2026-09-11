using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class YearRecapShareSnapshotWriterTests
{
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
}
