using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class VisitRecapSharePreviewBuilderTests
{
    [Fact]
    public async Task BuildAsync_ShouldExposeOnlyTheExplicitPublicSelection()
    {
        VisitRecapSourceData source = new VisitRecapSourceData(
            "park-technical-id",
            VisitDate.ForDay(2026, 7, 26),
            RatingValue.FromDouble(4.5),
            new VisitRecapSourceRevision(7, true),
            new[]
            {
                new VisitRecapSourceOccurrence(
                    "item-a",
                    RideOccurrenceStatus.Completed,
                    "Historical A",
                    ParkItemCategory.Attraction,
                    RatingValue.FromDouble(4.5)),
                new VisitRecapSourceOccurrence(
                    "item-a",
                    RideOccurrenceStatus.Completed,
                    "Historical A",
                    ParkItemCategory.Attraction,
                    RatingValue.FromDouble(5)),
                new VisitRecapSourceOccurrence(
                    "item-b",
                    RideOccurrenceStatus.MissedClosed,
                    "Historical B",
                    ParkItemCategory.Attraction,
                    null),
            });
        Mock<IVisitRecapSourceReader> sourceReader =
            new Mock<IVisitRecapSourceReader>(MockBehavior.Strict);
        sourceReader.Setup(value => value.GetOwnedCompletedRevisionAsync(
                "owner-1",
                "visit-1",
                CancellationToken.None))
            .ReturnsAsync(source.Revision);
        sourceReader.Setup(value => value.GetOwnedCompletedAsync(
                "owner-1",
                "visit-1",
                CancellationToken.None))
            .ReturnsAsync(source);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [PersonalRankingShareSourceScope.PublicCatalog] = new ShareSourceRevision(
                    3,
                    0,
                    new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc)),
            });
        Mock<IParkNameReadRepository> parks =
            new Mock<IParkNameReadRepository>(MockBehavior.Strict);
        parks.Setup(value => value.GetNamesByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["park-technical-id"] = "Denain Évasion",
            });
        Mock<IVisitTargetResolver> targets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targets.Setup(value => value.ResolveAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                ["item-a"] = new VisitTarget(
                    "item-a",
                    "park-technical-id",
                    "Le Galion",
                    ParkItemCategory.Attraction,
                    null,
                    null),
                ["item-b"] = new VisitTarget(
                    "item-b",
                    "park-technical-id",
                    "La Maison d’Houdini",
                    ParkItemCategory.Attraction,
                    null,
                    null),
            });
        VisitRecapSharePublicationSource publicationSource =
            new VisitRecapSharePublicationSource(sourceReader.Object, revisions.Object);
        VisitRecapSharePreviewBuilder builder = new VisitRecapSharePreviewBuilder(
            sourceReader.Object,
            publicationSource,
            parks.Object,
            targets.Object);
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Month,
            new[]
            {
                ShareContentField.RideCount,
                ShareContentField.TemporalRatings,
                ShareContentField.MissedItems,
                ShareContentField.PublicCaption,
            });

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            "visit-1",
            policy,
            new VisitRecapShareInput(new[] { "item-a" }, "Une belle journée"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.SourceVersion);
        Assert.Equal(64, result.Value.ContentFingerprint.Length);
        VisitRecapSharePreviewResult recap = Assert.IsType<VisitRecapSharePreviewResult>(
            result.Value.VisitRecap);
        Assert.Equal("Denain Évasion", recap.ParkName);
        Assert.Equal(ShareDatePrecision.Month, recap.Date!.Precision);
        Assert.Equal(2, recap.TotalRideCount);
        Assert.Equal(1, recap.DistinctItemCount);
        Assert.Equal(4.5, recap.ParkRating);
        Assert.Equal("Une belle journée", recap.PublicCaption);
        VisitRecapShareItemResult item = Assert.Single(recap.Items);
        Assert.Equal("Le Galion", item.Name);
        Assert.Equal(2, item.RideCount);
        Assert.Equal(4.75, item.AverageRating);
        Assert.DoesNotContain("La Maison d’Houdini", JsonSerializer.Serialize(recap), StringComparison.Ordinal);
        sourceReader.Verify(value => value.GetOwnedCompletedRevisionAsync(
            "owner-1",
            "visit-1",
            CancellationToken.None), Times.Exactly(2));
        sourceReader.VerifyAll();
        revisions.Verify(value => value.GetSnapshotAsync(
            It.IsAny<IReadOnlyCollection<string>>(),
            CancellationToken.None), Times.Exactly(2));
        revisions.VerifyAll();
        parks.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task BuildAsync_WhenRequestedDateIsMorePreciseThanTheVisit_ShouldRejectIt()
    {
        VisitRecapSourceRevision revision = new VisitRecapSourceRevision(2, true);
        Mock<IVisitRecapSourceReader> sourceReader =
            new Mock<IVisitRecapSourceReader>(MockBehavior.Strict);
        sourceReader.Setup(value => value.GetOwnedCompletedRevisionAsync(
                "owner-1",
                "visit-1",
                CancellationToken.None))
            .ReturnsAsync(revision);
        sourceReader.Setup(value => value.GetOwnedCompletedAsync(
                "owner-1",
                "visit-1",
                CancellationToken.None))
            .ReturnsAsync(new VisitRecapSourceData(
                "park-1",
                VisitDate.ForYear(2025),
                null,
                revision,
                Array.Empty<VisitRecapSourceOccurrence>()));
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [PersonalRankingShareSourceScope.PublicCatalog] = new ShareSourceRevision(
                    0,
                    0,
                    new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc)),
            });
        VisitRecapSharePublicationSource publicationSource =
            new VisitRecapSharePublicationSource(sourceReader.Object, revisions.Object);
        VisitRecapSharePreviewBuilder builder = new VisitRecapSharePreviewBuilder(
            sourceReader.Object,
            publicationSource,
            Mock.Of<IParkNameReadRepository>(MockBehavior.Strict),
            Mock.Of<IVisitTargetResolver>(MockBehavior.Strict));

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            "visit-1",
            ShareContentPolicy.Create(
                SharePublicationType.VisitRecap,
                ShareDatePrecision.Day,
                Array.Empty<ShareContentField>()),
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.visit-recap-selection-invalid");
        sourceReader.VerifyAll();
        revisions.VerifyAll();
    }
}
