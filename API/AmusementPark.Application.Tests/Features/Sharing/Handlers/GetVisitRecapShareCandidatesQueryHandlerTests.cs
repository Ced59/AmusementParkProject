using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

public sealed class GetVisitRecapShareCandidatesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRestoreTheCurrentSnapshotSelectionAndCaption()
    {
        DateTime nowUtc = new DateTime(2026, 9, 7, 5, 0, 0, DateTimeKind.Utc);
        SharePublicationId publicationId = SharePublicationId.Parse("publication-1");
        string sourceScopeKey = VisitRecapShareSourceScope.Create("owner-1", "visit-1");
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Month,
            new[] { ShareContentField.RideCount, ShareContentField.PublicCaption });
        SharePublication publication = SharePublication.Restore(
            publicationId,
            "owner-1",
            SharePublicationType.VisitRecap,
            sourceScopeKey,
            ShareToken.Parse(new string('A', ShareToken.EncodedLength)),
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            policy,
            7,
            1,
            1,
            nowUtc,
            null,
            nowUtc,
            nowUtc,
            "fingerprint");
        VisitRecapShareItemResult savedItem = new VisitRecapShareItemResult(
            "item-b",
            "La Roue",
            "Attraction",
            1,
            null,
            false);
        VisitRecapShareSnapshot snapshot = new VisitRecapShareSnapshot(
            publicationId,
            1,
            1,
            7,
            1,
            ShareDatePrecision.Month,
            policy.IncludedFields,
            "fingerprint",
            new VisitRecapSharePreviewResult(
                "park-1",
                "Parc public",
                null,
                1,
                1,
                new[] { "Attraction" },
                null,
                null,
                null,
                new[] { savedItem },
                "Souvenir déjà public",
                true,
                false,
                false),
            nowUtc);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetOwnedBySourceAsync(
                "owner-1",
                SharePublicationType.VisitRecap,
                sourceScopeKey,
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<IVisitRecapShareSnapshotRepository> snapshots =
            new Mock<IVisitRecapShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.GetAsync(
                publicationId,
                1,
                CancellationToken.None))
            .ReturnsAsync(snapshot);
        Mock<IVisitRecapSharePreviewBuilder> previewBuilder =
            new Mock<IVisitRecapSharePreviewBuilder>(MockBehavior.Strict);
        previewBuilder.Setup(value => value.GetCandidatesAsync(
                "owner-1",
                "visit-1",
                false,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-b" })),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<VisitRecapShareCandidatesResult>.Success(
                new VisitRecapShareCandidatesResult(
                    new[] { savedItem },
                    1,
                    false)));
        GetVisitRecapShareCandidatesQueryHandler handler =
            new GetVisitRecapShareCandidatesQueryHandler(
                previewBuilder.Object,
                publications.Object,
                snapshots.Object);

        ApplicationResult<VisitRecapShareCandidatesResult> result =
            await handler.HandleAsync(
                new GetVisitRecapShareCandidatesQuery(
                    "owner-1",
                    "visit-1",
                    false),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasSavedSnapshot);
        Assert.Equal("Souvenir déjà public", result.Value.SavedPublicCaption);
        Assert.Equal(new[] { "item-b" }, result.Value.SavedSelectedParkItemIds);
        publications.VerifyAll();
        snapshots.VerifyAll();
        previewBuilder.VerifyAll();
    }
}
