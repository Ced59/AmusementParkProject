using System.Text.Json;
using AmusementPark.Application.Errors;
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
        Mock<IVisitRecapPublicParkReader> parks =
            new Mock<IVisitRecapPublicParkReader>(MockBehavior.Strict);
        parks.Setup(value => value.GetVisibleNameAsync(
                "park-technical-id",
                CancellationToken.None))
            .ReturnsAsync("Denain Évasion");
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
            Mock.Of<IVisitRecapPublicParkReader>(MockBehavior.Strict),
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

    [Fact]
    public async Task BuildAsync_WhenParkIsHidden_ShouldRejectThePublicRecap()
    {
        VisitRecapSourceData source = new VisitRecapSourceData(
            "park-hidden",
            VisitDate.ForDay(2026, 7, 26),
            null,
            new VisitRecapSourceRevision(4, true),
            Array.Empty<VisitRecapSourceOccurrence>());
        VisitRecapSharePreviewBuilder builder = CreateStableBuilder(
            source,
            null,
            new Dictionary<string, VisitTarget>());

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            "visit-1",
            ShareContentPolicy.Create(
                SharePublicationType.VisitRecap,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.RideCount }),
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.source-unavailable");
    }

    [Fact]
    public async Task BuildAsync_WhenCurrentItemIsHidden_ShouldNeverExposeItsCatalogMetadata()
    {
        VisitRecapSourceData source = new VisitRecapSourceData(
            "park-1",
            VisitDate.ForDay(2026, 7, 26),
            null,
            new VisitRecapSourceRevision(4, true),
            new[]
            {
                new VisitRecapSourceOccurrence(
                    "item-hidden",
                    RideOccurrenceStatus.Completed,
                    "Ancien nom",
                    ParkItemCategory.Attraction,
                    null),
            });
        VisitTarget hiddenTarget = new VisitTarget(
            "item-hidden",
            "park-1",
            "Métadonnée courante masquée",
            ParkItemCategory.Attraction,
            null,
            null,
            IsVisible: false);
        VisitRecapSharePreviewBuilder builder = CreateStableBuilder(
            source,
            "Parc public",
            new Dictionary<string, VisitTarget>
            {
                [hiddenTarget.ParkItemId] = hiddenTarget,
            });

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            "visit-1",
            ShareContentPolicy.Create(
                SharePublicationType.VisitRecap,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.RideCount }),
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        VisitRecapSharePreviewResult recap = Assert.IsType<VisitRecapSharePreviewResult>(
            result.Value!.VisitRecap);
        Assert.Empty(recap.Items);
        Assert.Equal(0, recap.TotalRideCount);
        Assert.True(recap.HasIncompleteItems);
        Assert.DoesNotContain(
            "Métadonnée courante masquée",
            JsonSerializer.Serialize(recap),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildAsync_WhenImplicitSelectionExceedsTheItemCap_ShouldRejectIt()
    {
        VisitRecapSourceOccurrence[] occurrences = Enumerable.Range(
                1,
                VisitRecapShareInputNormalizer.MaximumSelectedItemCount + 1)
            .Select(index => new VisitRecapSourceOccurrence(
                $"item-{index}",
                RideOccurrenceStatus.Completed,
                $"Attraction {index}",
                ParkItemCategory.Attraction,
                null))
            .ToArray();
        VisitRecapSourceData source = new VisitRecapSourceData(
            "park-1",
            VisitDate.ForDay(2026, 7, 26),
            null,
            new VisitRecapSourceRevision(4, true),
            occurrences);
        VisitRecapSharePreviewBuilder builder = CreateStableBuilder(
            source,
            "Parc public",
            new Dictionary<string, VisitTarget>());

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            "visit-1",
            ShareContentPolicy.Create(
                SharePublicationType.VisitRecap,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.RideCount }),
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.visit-recap-too-large");
    }

    private static VisitRecapSharePreviewBuilder CreateStableBuilder(
        VisitRecapSourceData source,
        string? publicParkName,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        Mock<IVisitRecapSourceReader> sourceReader = new Mock<IVisitRecapSourceReader>();
        sourceReader.Setup(value => value.GetOwnedCompletedAsync(
                "owner-1",
                "visit-1",
                CancellationToken.None))
            .ReturnsAsync(source);
        Mock<IVisitRecapShareSourceVersionProvider> versions =
            new Mock<IVisitRecapShareSourceVersionProvider>();
        versions.Setup(value => value.GetOwnedSourceVersionAsync(
                "owner-1",
                "visit-1",
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<long>.Success(source.Revision.Version));
        Mock<IVisitRecapPublicParkReader> parks = new Mock<IVisitRecapPublicParkReader>();
        parks.Setup(value => value.GetVisibleNameAsync(
                source.ParkId,
                CancellationToken.None))
            .ReturnsAsync(publicParkName);
        Mock<IVisitTargetResolver> targetResolver = new Mock<IVisitTargetResolver>();
        targetResolver.Setup(value => value.ResolveAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(targets);
        return new VisitRecapSharePreviewBuilder(
            sourceReader.Object,
            versions.Object,
            parks.Object,
            targetResolver.Object);
    }
}
