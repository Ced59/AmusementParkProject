using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

public sealed class GetSharedYearRecapQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnOnlyTheFrozenAndRevalidatedSnapshot()
    {
        ShareContentPolicy policy = CreatePolicy();
        ResolvedSharePublicationResult publication = CreatePublication(policy);
        YearRecapShareSnapshot snapshot = CreateSnapshot(policy, "fingerprint-a");
        Mock<ISharePublicationAccessResolver> resolver =
            new Mock<ISharePublicationAccessResolver>(MockBehavior.Strict);
        resolver.Setup(value => value.ResolveAsync(
                "opaque-share-id",
                SharePublicationType.YearRecap,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ResolvedSharePublicationResult>.Success(publication));
        resolver.Setup(value => value.RevalidateAsync(
                "opaque-share-id",
                publication,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<bool>.Success(true));
        Mock<IYearRecapShareSnapshotRepository> snapshots =
            new Mock<IYearRecapShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.GetAsync(
                SharePublicationId.Parse("publication-1"),
                3,
                CancellationToken.None))
            .ReturnsAsync(snapshot);
        GetSharedYearRecapQueryHandler handler = new GetSharedYearRecapQueryHandler(
            resolver.Object,
            snapshots.Object);

        ApplicationResult<SharedYearRecapResult> result = await handler.HandleAsync(
            new GetSharedYearRecapQuery("opaque-share-id"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2026, result.Value!.Content.Year);
        Assert.Equal(snapshot.Content, result.Value.Content);
        resolver.VerifyAll();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSnapshotPolicyDiffers_ShouldReturnNotFoundWithoutServingIt()
    {
        ShareContentPolicy policy = CreatePolicy();
        ResolvedSharePublicationResult publication = CreatePublication(policy);
        YearRecapShareSnapshot mismatched = CreateSnapshot(
            ShareContentPolicy.Create(
                SharePublicationType.YearRecap,
                ShareDatePrecision.Year,
                new[] { ShareContentField.RideCount }),
            "fingerprint-a");
        Mock<ISharePublicationAccessResolver> resolver =
            new Mock<ISharePublicationAccessResolver>(MockBehavior.Strict);
        resolver.Setup(value => value.ResolveAsync(
                "opaque-share-id",
                SharePublicationType.YearRecap,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ResolvedSharePublicationResult>.Success(publication));
        Mock<IYearRecapShareSnapshotRepository> snapshots =
            new Mock<IYearRecapShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.GetAsync(
                SharePublicationId.Parse("publication-1"),
                3,
                CancellationToken.None))
            .ReturnsAsync(mismatched);
        GetSharedYearRecapQueryHandler handler = new GetSharedYearRecapQueryHandler(
            resolver.Object,
            snapshots.Object);

        ApplicationResult<SharedYearRecapResult> result = await handler.HandleAsync(
            new GetSharedYearRecapQuery("opaque-share-id"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.snapshot-unavailable");
        resolver.Verify(value => value.RevalidateAsync(
            It.IsAny<string>(),
            It.IsAny<ResolvedSharePublicationResult>(),
            It.IsAny<CancellationToken>()), Times.Never);
        resolver.VerifyAll();
        snapshots.VerifyAll();
    }

    private static ShareContentPolicy CreatePolicy()
    {
        return ShareContentPolicy.Create(
            SharePublicationType.YearRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.RideCount, ShareContentField.PublicCaption });
    }

    private static ResolvedSharePublicationResult CreatePublication(ShareContentPolicy policy)
    {
        return new ResolvedSharePublicationResult(
            "owner-1",
            null,
            SharePublicationType.YearRecap,
            policy,
            new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc),
            "scope-1",
            12,
            3,
            "publication-1",
            "fingerprint-a");
    }

    private static YearRecapShareSnapshot CreateSnapshot(
        ShareContentPolicy policy,
        string fingerprint)
    {
        return new YearRecapShareSnapshot(
            SharePublicationId.Parse("publication-1"),
            3,
            7,
            12,
            policy.SchemaVersion,
            policy.DatePrecision,
            policy.IncludedFields,
            fingerprint,
            CreateContent(),
            new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc));
    }

    private static YearRecapSharePreviewResult CreateContent()
    {
        return new YearRecapSharePreviewResult(
            2026,
            null,
            2,
            0,
            0,
            4,
            2,
            null,
            new[] { "Attraction" },
            null,
            null,
            Array.Empty<YearRecapShareParkResult>(),
            null,
            null,
            null,
            Array.Empty<YearRecapShareHighlightResult>(),
            "Une belle année",
            false,
            "passport-year-recap-v1",
            false);
    }
}
