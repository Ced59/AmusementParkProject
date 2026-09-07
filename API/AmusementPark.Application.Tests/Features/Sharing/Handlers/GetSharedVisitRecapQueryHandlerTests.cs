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

public sealed class GetSharedVisitRecapQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnOnlyTheFrozenSnapshotForTheResolvedRevision()
    {
        ShareContentPolicy policy = CreatePolicy();
        ResolvedSharePublicationResult publication = CreatePublication(policy);
        VisitRecapShareSnapshot snapshot = CreateSnapshot(policy, "fingerprint-a");
        Mock<ISharePublicationAccessResolver> resolver =
            new Mock<ISharePublicationAccessResolver>(MockBehavior.Strict);
        resolver.Setup(value => value.ResolveAsync(
                "opaque-share-id",
                SharePublicationType.VisitRecap,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ResolvedSharePublicationResult>.Success(publication));
        resolver.Setup(value => value.RevalidateAsync(
                "opaque-share-id",
                publication,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<bool>.Success(true));
        Mock<IVisitRecapShareSnapshotRepository> snapshots =
            new Mock<IVisitRecapShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.GetAsync(
                SharePublicationId.Parse("publication-1"),
                3,
                CancellationToken.None))
            .ReturnsAsync(snapshot);
        GetSharedVisitRecapQueryHandler handler = new GetSharedVisitRecapQueryHandler(
            resolver.Object,
            snapshots.Object);

        ApplicationResult<SharedVisitRecapResult> result = await handler.HandleAsync(
            new GetSharedVisitRecapQuery("opaque-share-id"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Denain Évasion", result.Value!.Content.ParkName);
        Assert.Equal(snapshot.Content, result.Value.Content);
        resolver.VerifyAll();
        snapshots.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSnapshotSelectionDoesNotMatchPublication_ShouldReturnNotFound()
    {
        ShareContentPolicy policy = CreatePolicy();
        ResolvedSharePublicationResult publication = CreatePublication(policy);
        Mock<ISharePublicationAccessResolver> resolver =
            new Mock<ISharePublicationAccessResolver>(MockBehavior.Strict);
        resolver.Setup(value => value.ResolveAsync(
                "opaque-share-id",
                SharePublicationType.VisitRecap,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ResolvedSharePublicationResult>.Success(publication));
        Mock<IVisitRecapShareSnapshotRepository> snapshots =
            new Mock<IVisitRecapShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.GetAsync(
                SharePublicationId.Parse("publication-1"),
                3,
                CancellationToken.None))
            .ReturnsAsync(CreateSnapshot(policy, "another-fingerprint"));
        GetSharedVisitRecapQueryHandler handler = new GetSharedVisitRecapQueryHandler(
            resolver.Object,
            snapshots.Object);

        ApplicationResult<SharedVisitRecapResult> result = await handler.HandleAsync(
            new GetSharedVisitRecapQuery("opaque-share-id"),
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
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Month,
            new[] { ShareContentField.RideCount, ShareContentField.PublicCaption });
    }

    private static ResolvedSharePublicationResult CreatePublication(
        ShareContentPolicy policy)
    {
        return new ResolvedSharePublicationResult(
            "owner-1",
            null,
            SharePublicationType.VisitRecap,
            policy,
            new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            "scope-1",
            12,
            3,
            "publication-1",
            "fingerprint-a");
    }

    private static VisitRecapShareSnapshot CreateSnapshot(
        ShareContentPolicy policy,
        string fingerprint)
    {
        return new VisitRecapShareSnapshot(
            SharePublicationId.Parse("publication-1"),
            3,
            7,
            12,
            policy.SchemaVersion,
            policy.DatePrecision,
            policy.IncludedFields,
            fingerprint,
            new VisitRecapSharePreviewResult(
                "park-1",
                "Denain Évasion",
                new VisitRecapShareDateResult(
                    2026,
                    7,
                    null,
                    ShareDatePrecision.Month,
                    false),
                1,
                2,
                new[] { "Attraction" },
                null,
                null,
                null,
                Array.Empty<VisitRecapShareItemResult>(),
                "Un beau souvenir",
                false,
                false,
                false),
            new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc));
    }
}
