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

public sealed class YearRecapSharePreviewBuilderTests
{
    [Fact]
    public async Task BuildAsync_ShouldCreateAnExplainableRecapFromCompletedPublicDataOnly()
    {
        YearRecapSourceData source = CreateSource();
        Mock<IYearRecapSourceReader> sourceReader = new Mock<IYearRecapSourceReader>(MockBehavior.Strict);
        sourceReader.Setup(value => value.ReadOwnedCompletedYearAsync(
                "owner-1",
                2026,
                CancellationToken.None))
            .ReturnsAsync(source);
        Mock<IYearRecapShareSourceVersionProvider> versions =
            new Mock<IYearRecapShareSourceVersionProvider>(MockBehavior.Strict);
        versions.Setup(value => value.GetOwnedSourceVersionAsync(
                "owner-1",
                2026,
                CancellationToken.None))
            .ReturnsAsync(SourceRevision(42, source.SourceFingerprint));
        Mock<IVisitRecapPublicParkReader> parks =
            new Mock<IVisitRecapPublicParkReader>(MockBehavior.Strict);
        parks.Setup(value => value.GetVisibleNamesAsync(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.OrderBy(static id => id).SequenceEqual(new[] { "park-hidden", "park-public" })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["park-public"] = "Parc public",
            });
        Mock<IVisitTargetResolver> targets = new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targets.Setup(value => value.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.OrderBy(static id => id).SequenceEqual(new[] { "item-a", "item-old" })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                ["item-a"] = new VisitTarget(
                    "item-a",
                    "park-public",
                    "Le Galion",
                    ParkItemCategory.Attraction,
                    null,
                    null),
            });
        YearRecapSharePreviewBuilder builder = new YearRecapSharePreviewBuilder(
            sourceReader.Object,
            versions.Object,
            parks.Object,
            targets.Object);
        ShareContentPolicy policy = CreateFullPolicy();

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            2026,
            policy,
            new YearRecapShareInput("Une belle année"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value!.SourceVersion);
        Assert.Equal(64, result.Value.ContentFingerprint.Length);
        YearRecapSharePreviewResult recap = Assert.IsType<YearRecapSharePreviewResult>(
            result.Value.YearRecap);
        Assert.Equal(3, recap.VisitCount);
        Assert.Equal(1, recap.ParkCount);
        Assert.Equal(1, recap.ApproximateVisitCount);
        Assert.Equal(1d / 3d, recap.ApproximateVisitRate, 8);
        Assert.Equal(4, recap.TotalRideCount);
        Assert.Equal(2, recap.DistinctItemCount);
        Assert.Equal(1, recap.MissedItemCount);
        Assert.Equal("Parc public", Assert.Single(recap.MostVisitedParks).Name);
        Assert.Equal(4.5d, recap.ParkRatings!.Average);
        Assert.Equal(3, recap.ParkRatings.EligibleCount);
        Assert.Equal(2, recap.ParkRatings.RatedCount);
        Assert.Equal(11d / 3d, recap.RideRatings!.Average!.Value, 8);
        Assert.Equal("Le Galion", recap.MostRepeatedItem!.Name);
        Assert.Equal(3, recap.MostRepeatedItem.RideCount);
        Assert.Equal("Le Galion", recap.RatingEvolution!.Name);
        Assert.Equal("Rising", recap.RatingEvolution.Kind);
        Assert.Equal("Attraction disparue", Assert.Single(recap.NowClosedItems).Name);
        Assert.Equal("Une belle année", recap.PublicCaption);
        Assert.True(recap.HasIncompleteCatalog);
        Assert.False(recap.IsEmpty);
        string json = JsonSerializer.Serialize(recap);
        Assert.DoesNotContain("park-public", json, StringComparison.Ordinal);
        Assert.DoesNotContain("park-hidden", json, StringComparison.Ordinal);
        Assert.DoesNotContain("item-a", json, StringComparison.Ordinal);
        sourceReader.VerifyAll();
        versions.Verify(value => value.GetOwnedSourceVersionAsync(
            "owner-1",
            2026,
            CancellationToken.None), Times.Exactly(2));
        parks.VerifyAll();
        targets.VerifyAll();
    }

    [Fact]
    public async Task BuildAsync_WhenNoCompletedPublicVisitExists_ShouldReturnANonPublishableEmptyPreview()
    {
        YearRecapSharePreviewBuilder builder = CreateEmptyBuilder();

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            2026,
            ShareContentPolicy.Create(
                SharePublicationType.YearRecap,
                ShareDatePrecision.Year,
                new[] { ShareContentField.RideCount }),
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        YearRecapSharePreviewResult recap = Assert.IsType<YearRecapSharePreviewResult>(
            result.Value!.YearRecap);
        Assert.True(recap.IsEmpty);
        Assert.Equal(0, recap.VisitCount);
        Assert.Empty(recap.MostVisitedParks);
    }

    [Fact]
    public async Task BuildAsync_WhenTheSourceChangesDuringPreview_ShouldRejectThePreview()
    {
        YearRecapSourceData source = new YearRecapSourceData(
            Array.Empty<PassportVisitStatisticsObservation>(),
            Array.Empty<PassportRideStatisticsObservation>(),
            new Dictionary<string, string?>(),
            "source-between",
            true);
        Mock<IYearRecapSourceReader> sourceReader = new Mock<IYearRecapSourceReader>();
        sourceReader.Setup(value => value.ReadOwnedCompletedYearAsync(
                "owner-1",
                2026,
                CancellationToken.None))
            .ReturnsAsync(source);
        Mock<IYearRecapShareSourceVersionProvider> versions =
            new Mock<IYearRecapShareSourceVersionProvider>();
        versions.SetupSequence(value => value.GetOwnedSourceVersionAsync(
                "owner-1",
                2026,
                CancellationToken.None))
            .ReturnsAsync(SourceRevision(1, "source-before"))
            .ReturnsAsync(SourceRevision(2, "source-after"));
        Mock<IVisitRecapPublicParkReader> parks = new Mock<IVisitRecapPublicParkReader>();
        parks.Setup(value => value.GetVisibleNamesAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string>());
        YearRecapSharePreviewBuilder builder = new YearRecapSharePreviewBuilder(
            sourceReader.Object,
            versions.Object,
            parks.Object,
            Mock.Of<IVisitTargetResolver>(MockBehavior.Strict));

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            2026,
            ShareContentPolicy.Create(
                SharePublicationType.YearRecap,
                ShareDatePrecision.Year,
                Array.Empty<ShareContentField>()),
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.source-changed");
    }

    private static ShareContentPolicy CreateFullPolicy()
    {
        return ShareContentPolicy.Create(
            SharePublicationType.YearRecap,
            ShareDatePrecision.Year,
            new[]
            {
                ShareContentField.RideCount,
                ShareContentField.TemporalRatings,
                ShareContentField.GeographicStatistics,
                ShareContentField.MissedItems,
                ShareContentField.PublicCaption,
            });
    }

    private static YearRecapSourceData CreateSource()
    {
        PassportVisitStatisticsObservation[] visits =
        {
            new PassportVisitStatisticsObservation(
                "visit-1", "park-public", VisitDate.ForDay(2026, 4, 1), RatingValue.FromDouble(4)),
            new PassportVisitStatisticsObservation(
                "visit-2", "park-public", new VisitDate(2026, 5, null, VisitDatePrecision.Month, true), null),
            new PassportVisitStatisticsObservation(
                "visit-3", "park-public", VisitDate.ForDay(2026, 6, 1), RatingValue.FromDouble(5)),
            new PassportVisitStatisticsObservation(
                "visit-hidden", "park-hidden", VisitDate.ForDay(2026, 7, 1), RatingValue.FromDouble(1)),
        };
        PassportRideStatisticsObservation[] rides =
        {
            CreateRide("ride-1", "visit-1", "park-public", "item-a", 2026, 4, 1, RideOccurrenceStatus.Completed, 2),
            CreateRide("ride-2", "visit-2", "park-public", "item-a", 2026, 5, 1, RideOccurrenceStatus.Completed, 4),
            CreateRide("ride-3", "visit-3", "park-public", "item-a", 2026, 6, 1, RideOccurrenceStatus.Completed, 5),
            CreateRide("ride-old", "visit-3", "park-public", "item-old", 2026, 6, 1, RideOccurrenceStatus.Completed, null),
            CreateRide("ride-missed", "visit-3", "park-public", "item-old", 2026, 6, 1, RideOccurrenceStatus.MissedClosed, null),
            CreateRide("ride-hidden", "visit-hidden", "park-hidden", "item-hidden", 2026, 7, 1, RideOccurrenceStatus.Completed, 5),
        };
        return new YearRecapSourceData(
            visits,
            rides,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["item-a"] = "Ancien Galion",
                ["item-old"] = "Attraction disparue",
                ["item-hidden"] = "Attraction masquée",
            },
            "source-current",
            true);
    }

    private static PassportRideStatisticsObservation CreateRide(
        string rideId,
        string visitId,
        string parkId,
        string itemId,
        int year,
        int month,
        int day,
        RideOccurrenceStatus status,
        double? rating)
    {
        return new PassportRideStatisticsObservation(
            rideId,
            visitId,
            parkId,
            itemId,
            VisitDate.ForDay(year, month, day),
            status,
            rating.HasValue ? RatingValue.FromDouble(rating.Value) : null,
            ParkItemCategory.Attraction.ToString(),
            null);
    }

    private static YearRecapSharePreviewBuilder CreateEmptyBuilder()
    {
        Mock<IYearRecapSourceReader> sourceReader = new Mock<IYearRecapSourceReader>();
        sourceReader.Setup(value => value.ReadOwnedCompletedYearAsync(
                "owner-1",
                2026,
                CancellationToken.None))
            .ReturnsAsync(new YearRecapSourceData(
                Array.Empty<PassportVisitStatisticsObservation>(),
                Array.Empty<PassportRideStatisticsObservation>(),
                new Dictionary<string, string?>(),
                "source-empty",
                true));
        Mock<IYearRecapShareSourceVersionProvider> versions =
            new Mock<IYearRecapShareSourceVersionProvider>();
        versions.Setup(value => value.GetOwnedSourceVersionAsync(
                "owner-1",
                2026,
                CancellationToken.None))
            .ReturnsAsync(SourceRevision(1, "source-empty"));
        Mock<IVisitRecapPublicParkReader> parks = new Mock<IVisitRecapPublicParkReader>();
        parks.Setup(value => value.GetVisibleNamesAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string>());
        return new YearRecapSharePreviewBuilder(
            sourceReader.Object,
            versions.Object,
            parks.Object,
            Mock.Of<IVisitTargetResolver>(MockBehavior.Strict));
    }

    private static ApplicationResult<YearRecapShareSourceRevision> SourceRevision(
        long version,
        string sourceFingerprint)
    {
        return ApplicationResult<YearRecapShareSourceRevision>.Success(
            new YearRecapShareSourceRevision(version, sourceFingerprint));
    }
}
