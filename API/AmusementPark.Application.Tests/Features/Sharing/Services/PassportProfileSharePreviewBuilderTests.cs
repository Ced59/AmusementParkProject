using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileSharePreviewBuilderTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task BuildAsync_ShouldExposeOnlyTheSelectedPublicStoryWithoutTechnicalIds()
    {
        PassportProfileSourceData source = CreateSource();
        Mock<IPassportProfileSourceReader> sourceReader =
            new Mock<IPassportProfileSourceReader>(MockBehavior.Strict);
        sourceReader.Setup(value => value.ReadOwnedCompletedPassportAsync(
                "owner-technical-id",
                CancellationToken.None))
            .ReturnsAsync(source);
        Mock<IPassportProfileShareSourceVersionProvider> versions =
            new Mock<IPassportProfileShareSourceVersionProvider>(MockBehavior.Strict);
        PassportProfileShareSourceRevisionSnapshot revisionSnapshot = CreateRevisionSnapshot(4, 5, 3);
        versions.Setup(value => value.PrepareOwnedSourceRevisionSnapshotAsync(
                "owner-technical-id",
                It.IsAny<ShareContentPolicy>(),
                It.Is<PassportProfileShareInput>(input =>
                    input.SelectedParkIds!.SequenceEqual(new[] { "park-public-id" })
                    && input.SelectedYears!.SequenceEqual(new[] { 2026 })),
                CancellationToken.None))
            .ReturnsAsync(
                ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Success(
                    revisionSnapshot));
        versions.Setup(value => value.GetOwnedSourceRevisionSnapshotAsync(
                "owner-technical-id",
                It.IsAny<ShareContentPolicy>(),
                It.Is<PassportProfileShareInput>(input =>
                    input.SelectedParkIds!.SequenceEqual(new[] { "park-public-id" })
                    && input.SelectedYears!.SequenceEqual(new[] { 2026 })),
                CancellationToken.None))
            .ReturnsAsync(
                ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Success(
                    revisionSnapshot));
        versions.Setup(value => value.ReconcileOwnedSourceVersionAsync(
                "owner-technical-id",
                source.SourceFingerprint,
                revisionSnapshot,
                It.IsAny<ShareContentPolicy>(),
                It.Is<PassportProfileShareInput>(input =>
                    input.SelectedParkIds!.SequenceEqual(new[] { "park-public-id" })
                    && input.SelectedYears!.SequenceEqual(new[] { 2026 })),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportProfileShareSourceRevision>.Success(
                new PassportProfileShareSourceRevision(12, source.SourceFingerprint)));
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IVisitTargetResolver> targets = CreateTargetResolver();
        Mock<IRatingRepository> ratings = CreateRatingRepository();
        Mock<IUserRepository> users = CreateUserRepository();
        Mock<IImageRepository> images = CreateImageRepository();
        PassportProfileSharePreviewBuilder builder = new PassportProfileSharePreviewBuilder(
            sourceReader.Object,
            versions.Object,
            parks.Object,
            targets.Object,
            ratings.Object,
            users.Object,
            images.Object);
        ShareContentPolicy policy = CreateFullPolicy();
        string selectedRating = PassportProfileRatingSelectionKey.Create(
            RatingTargetType.ParkItem,
            "item-technical-id");

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-technical-id",
            policy,
            new PassportProfileShareInput(
                new[] { 2026 },
                new[] { "park-public-id" },
                new[] { selectedRating },
                "Mon année la plus intense.",
                ShareVisibility.Public,
                true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        PassportProfileSharePreviewResult profile = Assert.IsType<PassportProfileSharePreviewResult>(
            result.Value!.PassportProfile);
        Assert.Equal("Camille", profile.DisplayName);
        Assert.Equal("/images/avatar-public-id", profile.AvatarUrl);
        Assert.Equal(1, profile.ParkCount);
        Assert.Equal(1, profile.VisitCount);
        Assert.Equal(1, profile.TotalRideCount);
        Assert.Equal("Parc public", Assert.Single(profile.Parks).Name);
        Assert.Equal("Attraction publique", Assert.Single(profile.PersonalRanking).Name);
        PassportProfileShareMissedItemResult missed = Assert.Single(profile.MissedItems);
        Assert.Equal("Attraction fermée", missed.Name);
        Assert.Equal("MissedClosure", missed.Status);
        Assert.Equal(1, missed.OccurrenceCount);
        Assert.True(profile.AllowsComparisons);
        Assert.False(profile.HasIncompleteCatalog);
        Assert.False(profile.IsEmpty);

        string serialized = JsonSerializer.Serialize(profile);
        Assert.DoesNotContain("owner-technical-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("park-public-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("visit-public-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("item-technical-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("rating-private-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-06-14", serialized, StringComparison.Ordinal);
        sourceReader.VerifyAll();
        sourceReader.Verify(value => value.ReadOwnedCompletedPassportAsync(
            "owner-technical-id",
            CancellationToken.None), Times.Once);
        versions.Verify(value => value.PrepareOwnedSourceRevisionSnapshotAsync(
            "owner-technical-id",
            It.IsAny<ShareContentPolicy>(),
            It.Is<PassportProfileShareInput>(input =>
                input.SelectedParkIds!.SequenceEqual(new[] { "park-public-id" })
                && input.SelectedYears!.SequenceEqual(new[] { 2026 })),
            CancellationToken.None), Times.Once);
        versions.Verify(value => value.GetOwnedSourceRevisionSnapshotAsync(
            "owner-technical-id",
            It.IsAny<ShareContentPolicy>(),
            It.Is<PassportProfileShareInput>(input =>
                input.SelectedParkIds!.SequenceEqual(new[] { "park-public-id" })
                && input.SelectedYears!.SequenceEqual(new[] { 2026 })),
            CancellationToken.None), Times.Once);
        versions.Verify(value => value.ReconcileOwnedSourceVersionAsync(
            "owner-technical-id",
            source.SourceFingerprint,
            revisionSnapshot,
            It.IsAny<ShareContentPolicy>(),
            It.Is<PassportProfileShareInput>(input =>
                input.SelectedParkIds!.SequenceEqual(new[] { "park-public-id" })
                && input.SelectedYears!.SequenceEqual(new[] { 2026 })),
            CancellationToken.None), Times.Once);
        parks.VerifyAll();
        targets.VerifyAll();
        ratings.VerifyAll();
        users.Verify(value => value.GetByIdAsync(
            "owner-technical-id",
            CancellationToken.None), Times.Exactly(2));
        images.Verify(value => value.GetCurrentByOwnerAuthoritativeAsync(
            ImageOwnerType.User,
            "owner-technical-id",
            ImageCategory.Avatar,
            CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task BuildAsync_WhenNoVisitMatchesTheSelection_ShouldRejectThePreview()
    {
        PassportProfileSourceData source = new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-public-id",
                    "park-public-id",
                    VisitDate.ForDay(2026, 6, 14),
                    null),
            },
            Array.Empty<PassportRideStatisticsObservation>(),
            "stable-fingerprint",
            true);
        PassportProfileSharePreviewBuilder builder = CreateBuilderWithoutOptionalContent(source);

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-technical-id",
            ShareContentPolicy.Create(
                SharePublicationType.PassportProfile,
                ShareDatePrecision.Year,
                new[] { ShareContentField.GeographicStatistics }),
            new PassportProfileShareInput(
                new[] { 2026 },
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                ShareVisibility.Unlisted,
                false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Code == "share-publication.passport-profile-selection-invalid");
    }

    [Fact]
    public async Task BuildAsync_WhenYearAndParkExistButNeverTogether_ShouldRejectThePreview()
    {
        PassportProfileSourceData source = new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-first-id",
                    "park-public-id",
                    VisitDate.ForDay(2025, 6, 14),
                    null),
                new PassportVisitStatisticsObservation(
                    "visit-second-id",
                    "park-second-id",
                    VisitDate.ForDay(2026, 6, 14),
                    null),
            },
            Array.Empty<PassportRideStatisticsObservation>(),
            "stable-fingerprint",
            true);

        ApplicationResult<SharePublicationPreviewResult> result =
            await CreateBuilderWithoutOptionalContent(source).BuildAsync(
                "owner-technical-id",
                ShareContentPolicy.Create(
                    SharePublicationType.PassportProfile,
                    ShareDatePrecision.Year,
                    new[] { ShareContentField.GeographicStatistics }),
                new PassportProfileShareInput(
                    new[] { 2025 },
                    new[] { "park-second-id" },
                    Array.Empty<string>(),
                    null,
                    ShareVisibility.Unlisted,
                    false),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Code == "share-publication.passport-profile-selection-invalid");
    }

    [Fact]
    public async Task BuildAsync_WhenRideFieldsArePrivate_ShouldNotReportCatalogLoss()
    {
        PassportProfileSourceData source = new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-public-id",
                    "park-public-id",
                    VisitDate.ForDay(2026, 6, 14),
                    null),
            },
            new[]
            {
                CreateRide("ride-hidden-id", "item-hidden-id", RideOccurrenceStatus.Completed, null),
            },
            "stable-fingerprint",
            true);
        PassportProfileSharePreviewBuilder builder = CreateBuilderWithoutOptionalContent(source);

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-technical-id",
            ShareContentPolicy.Create(
                SharePublicationType.PassportProfile,
                ShareDatePrecision.Year,
                new[] { ShareContentField.GeographicStatistics }),
            new PassportProfileShareInput(
                new[] { 2026 },
                new[] { "park-public-id" },
                Array.Empty<string>(),
                null,
                ShareVisibility.Unlisted,
                false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.PassportProfile!.HasIncompleteCatalog);
    }

    [Fact]
    public async Task BuildAsync_WhenMissedItemsAreSharedWithoutActivity_ShouldHideTheirCounts()
    {
        PassportProfileSourceData source = new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-public-id",
                    "park-public-id",
                    VisitDate.ForDay(2026, 6, 14),
                    null),
            },
            new[]
            {
                CreateRide(
                    "ride-missed-first",
                    "item-missed-id",
                    RideOccurrenceStatus.MissedClosed,
                    null,
                    "Attraction manquée"),
                CreateRide(
                    "ride-missed-second",
                    "item-missed-id",
                    RideOccurrenceStatus.MissedClosed,
                    null,
                    "Attraction manquée"),
            },
            "stable-fingerprint",
            true);

        ApplicationResult<SharePublicationPreviewResult> result =
            await CreateBuilderWithoutOptionalContent(source).BuildAsync(
                "owner-technical-id",
                ShareContentPolicy.Create(
                    SharePublicationType.PassportProfile,
                    ShareDatePrecision.Year,
                    new[] { ShareContentField.MissedItems }),
                new PassportProfileShareInput(
                    new[] { 2026 },
                    new[] { "park-public-id" },
                    Array.Empty<string>(),
                    null,
                    ShareVisibility.Unlisted,
                    false),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        PassportProfileShareMissedItemResult missed = Assert.Single(
            result.Value!.PassportProfile!.MissedItems);
        Assert.Null(missed.OccurrenceCount);
    }

    [Fact]
    public async Task BuildAsync_ShouldUseTheHistoricalNameFromTheSelectedVisitsOnly()
    {
        PassportProfileSourceData source = new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-selected",
                    "park-public-id",
                    VisitDate.ForDay(2026, 6, 14),
                    null),
                new PassportVisitStatisticsObservation(
                    "visit-excluded",
                    "park-public-id",
                    VisitDate.ForDay(2025, 6, 14),
                    null),
            },
            new[]
            {
                new PassportRideStatisticsObservation(
                    "ride-selected",
                    "visit-selected",
                    "park-public-id",
                    "item-hidden-id",
                    VisitDate.ForDay(2026, 6, 14),
                    RideOccurrenceStatus.MissedClosed,
                    null,
                    ParkItemCategory.Attraction.ToString(),
                    null,
                    "Nom pendant l’année sélectionnée"),
                new PassportRideStatisticsObservation(
                    "ride-excluded",
                    "visit-excluded",
                    "park-public-id",
                    "item-hidden-id",
                    VisitDate.ForDay(2025, 6, 14),
                    RideOccurrenceStatus.MissedClosed,
                    null,
                    ParkItemCategory.Attraction.ToString(),
                    null,
                    "Nom provenant d’une année exclue"),
            },
            "stable-fingerprint",
            true);

        ApplicationResult<SharePublicationPreviewResult> result =
            await CreateBuilderWithoutOptionalContent(source).BuildAsync(
                "owner-technical-id",
                ShareContentPolicy.Create(
                    SharePublicationType.PassportProfile,
                    ShareDatePrecision.Year,
                    new[] { ShareContentField.MissedItems }),
                new PassportProfileShareInput(
                    new[] { 2026 },
                    new[] { "park-public-id" },
                    Array.Empty<string>(),
                    null,
                    ShareVisibility.Unlisted,
                    false),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Nom pendant l’année sélectionnée",
            Assert.Single(result.Value!.PassportProfile!.MissedItems).Name);
    }

    [Fact]
    public async Task BuildAsync_ShouldGateTheYearBreakdownWithGeographicStatistics()
    {
        PassportProfileSourceData source = new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-public-id",
                    "park-public-id",
                    VisitDate.ForDay(2026, 6, 14),
                    null),
            },
            Array.Empty<PassportRideStatisticsObservation>(),
            "stable-fingerprint",
            true);
        PassportProfileShareInput input = new PassportProfileShareInput(
            new[] { 2026 },
            new[] { "park-public-id" },
            Array.Empty<string>(),
            null,
            ShareVisibility.Unlisted,
            false);

        ApplicationResult<SharePublicationPreviewResult> geographicResult =
            await CreateBuilderWithoutOptionalContent(source).BuildAsync(
                "owner-technical-id",
                ShareContentPolicy.Create(
                    SharePublicationType.PassportProfile,
                    ShareDatePrecision.Year,
                    new[] { ShareContentField.GeographicStatistics }),
                input,
                CancellationToken.None);
        ApplicationResult<SharePublicationPreviewResult> activityResult =
            await CreateBuilderWithoutOptionalContent(source).BuildAsync(
                "owner-technical-id",
                ShareContentPolicy.Create(
                    SharePublicationType.PassportProfile,
                    ShareDatePrecision.Year,
                    new[] { ShareContentField.RideCount }),
                input,
                CancellationToken.None);

        PassportProfileShareYearResult year = Assert.Single(
            geographicResult.Value!.PassportProfile!.Years);
        Assert.Equal(2026, year.Year);
        Assert.Null(year.CompletedRideCount);
        Assert.Empty(activityResult.Value!.PassportProfile!.Years);
        Assert.Equal(1, activityResult.Value.PassportProfile.VisitCount);
    }

    [Fact]
    public async Task BuildAsync_WhenNoSelectedFieldExposesContent_ShouldMarkPreviewEmpty()
    {
        PassportProfileSourceData source = new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-public-id",
                    "park-public-id",
                    VisitDate.ForDay(2026, 6, 14),
                    null),
            },
            Array.Empty<PassportRideStatisticsObservation>(),
            "stable-fingerprint",
            true);
        PassportProfileSharePreviewBuilder builder = CreateBuilderWithoutOptionalContent(source);

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-technical-id",
            ShareContentPolicy.Create(
                SharePublicationType.PassportProfile,
                ShareDatePrecision.Year,
                Array.Empty<ShareContentField>()),
            new PassportProfileShareInput(
                new[] { 2026 },
                new[] { "park-public-id" },
                Array.Empty<string>(),
                null,
                ShareVisibility.Unlisted,
                false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.PassportProfile!.IsEmpty);
    }

    [Fact]
    public async Task BuildAsync_WhenPublicNameIsMissing_ShouldPreserveTheLocalizedFallback()
    {
        PassportProfileSourceData source = new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-public-id",
                    "park-public-id",
                    VisitDate.ForDay(2026, 6, 14),
                    null),
            },
            Array.Empty<PassportRideStatisticsObservation>(),
            "stable-fingerprint",
            true);
        PassportProfileSharePreviewBuilder builder =
            CreateBuilderWithoutOptionalContent(source, null);

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-technical-id",
            ShareContentPolicy.Create(
                SharePublicationType.PassportProfile,
                ShareDatePrecision.Year,
                new[]
                {
                    ShareContentField.PublicDisplayName,
                    ShareContentField.GeographicStatistics,
                }),
            new PassportProfileShareInput(
                new[] { 2026 },
                new[] { "park-public-id" },
                Array.Empty<string>(),
                null,
                ShareVisibility.Unlisted,
                false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.PassportProfile!.DisplayName);
    }

    private static PassportProfileSharePreviewBuilder CreateBuilderWithoutOptionalContent(
        PassportProfileSourceData source,
        string? publicDisplayName = "Camille")
    {
        Mock<IPassportProfileSourceReader> sourceReader =
            new Mock<IPassportProfileSourceReader>();
        sourceReader.Setup(value => value.ReadOwnedCompletedPassportAsync(
                "owner-technical-id",
                CancellationToken.None))
            .ReturnsAsync(source);
        Mock<IPassportProfileShareSourceVersionProvider> versions =
            new Mock<IPassportProfileShareSourceVersionProvider>();
        PassportProfileShareSourceRevisionSnapshot revisionSnapshot = CreateRevisionSnapshot(1, 0, 0);
        versions.Setup(value => value.PrepareOwnedSourceRevisionSnapshotAsync(
                "owner-technical-id",
                It.IsAny<ShareContentPolicy>(),
                It.IsAny<PassportProfileShareInput>(),
                CancellationToken.None))
            .ReturnsAsync(
                ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Success(
                    revisionSnapshot));
        versions.Setup(value => value.GetOwnedSourceRevisionSnapshotAsync(
                "owner-technical-id",
                It.IsAny<ShareContentPolicy>(),
                It.IsAny<PassportProfileShareInput>(),
                CancellationToken.None))
            .ReturnsAsync(
                ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Success(
                    revisionSnapshot));
        versions.Setup(value => value.ReconcileOwnedSourceVersionAsync(
                "owner-technical-id",
                source.SourceFingerprint,
                revisionSnapshot,
                It.IsAny<ShareContentPolicy>(),
                It.IsAny<PassportProfileShareInput>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportProfileShareSourceRevision>.Success(
                new PassportProfileShareSourceRevision(1, source.SourceFingerprint)));
        Mock<IVisitTargetResolver> targets = new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targets.Setup(value => value.ResolveAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal));
        return new PassportProfileSharePreviewBuilder(
            sourceReader.Object,
            versions.Object,
            CreateParkRepository().Object,
            targets.Object,
            Mock.Of<IRatingRepository>(MockBehavior.Strict),
            CreateUserRepository(publicDisplayName).Object,
            Mock.Of<IImageRepository>(MockBehavior.Strict));
    }

    private static PassportProfileShareSourceRevisionSnapshot CreateRevisionSnapshot(
        long passportRevision,
        long identityRevision,
        long catalogRevision)
    {
        return new PassportProfileShareSourceRevisionSnapshot(
            new ShareSourceRevision(passportRevision, 0, NowUtc),
            new ShareSourceRevision(identityRevision, 0, NowUtc),
            new ShareSourceRevision(0, 0, NowUtc),
            new ShareSourceRevision(0, 0, NowUtc),
            new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [PublicCatalogShareSourceScope.CreatePark("park-public-id")] =
                    new ShareSourceRevision(catalogRevision, 0, NowUtc),
            });
    }

    private static PassportProfileSourceData CreateSource()
    {
        return new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-public-id",
                    "park-public-id",
                    VisitDate.ForDay(2026, 6, 14),
                    RatingValue.FromDouble(4.5)),
                new PassportVisitStatisticsObservation(
                    "visit-hidden-id",
                    "park-hidden-id",
                    VisitDate.ForDay(2025, 7, 1),
                    RatingValue.FromDouble(2)),
            },
            new[]
            {
                CreateRide("ride-completed-id", "item-technical-id", RideOccurrenceStatus.Completed, 4.5),
                CreateRide(
                    "ride-missed-id",
                    "item-closed-id",
                    RideOccurrenceStatus.MissedClosed,
                    null,
                    "Attraction fermée"),
                new PassportRideStatisticsObservation(
                    "ride-outside-scope-id",
                    "visit-hidden-id",
                    "park-hidden-id",
                    "item-outside-scope-id",
                    VisitDate.ForDay(2025, 4, 1),
                    RideOccurrenceStatus.Completed,
                    null,
                    ParkItemCategory.Attraction.ToString(),
                    ParkItemCategory.Attraction.ToString()),
            },
            "stable-fingerprint",
            true);
    }

    private static PassportRideStatisticsObservation CreateRide(
        string occurrenceId,
        string itemId,
        RideOccurrenceStatus status,
        double? rating,
        string? historicalName = null)
    {
        return new PassportRideStatisticsObservation(
            occurrenceId,
            "visit-public-id",
            "park-public-id",
            itemId,
            VisitDate.ForDay(2026, 6, 14),
            status,
            rating.HasValue ? RatingValue.FromDouble(rating.Value) : null,
            ParkItemCategory.Attraction.ToString(),
            ParkItemCategory.Attraction.ToString(),
            historicalName);
    }

    private static ShareContentPolicy CreateFullPolicy()
    {
        return ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.Avatar,
                ShareContentField.RideCount,
                ShareContentField.TemporalRatings,
                ShareContentField.GlobalRatings,
                ShareContentField.PublicCaption,
                ShareContentField.GeographicStatistics,
                ShareContentField.MissedItems,
            });
    }

    private static Mock<IParkRepository> CreateParkRepository()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(value => value.GetByIdsAsync(
                It.IsAny<IEnumerable<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-public-id",
                    Name = "Parc public",
                    CountryCode = "FR",
                    IsVisible = true,
                },
                new Park
                {
                    Id = "park-hidden-id",
                    Name = "Parc privé",
                    CountryCode = "BE",
                    IsVisible = false,
                },
                new Park
                {
                    Id = "park-second-id",
                    Name = "Second parc public",
                    CountryCode = "DE",
                    IsVisible = true,
                },
            });
        return parks;
    }

    private static Mock<IVisitTargetResolver> CreateTargetResolver()
    {
        Mock<IVisitTargetResolver> targets = new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        targets.Setup(value => value.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2
                    && ids.Contains("item-technical-id", StringComparer.Ordinal)
                    && ids.Contains("item-closed-id", StringComparer.Ordinal)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                ["item-technical-id"] = new VisitTarget(
                    "item-technical-id",
                    "park-public-id",
                    "Attraction publique",
                    ParkItemCategory.Attraction,
                    null,
                    null),
            });
        return targets;
    }

    private static Mock<IRatingRepository> CreateRatingRepository()
    {
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(value => value.GetVisibleUserRankingSourcesForParksAsync(
                "owner-technical-id",
                It.Is<IReadOnlyCollection<string>>(parkIds =>
                    parkIds.SequenceEqual(new[] { "park-public-id" })),
                101,
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new UserRatingListItemResult(
                    "rating-private-id",
                    RatingTargetType.ParkItem,
                    "item-technical-id",
                    "Attraction publique",
                    "park-public-id",
                    "Parc public",
                    ParkItemCategory.Attraction,
                    ParkItemType.RollerCoaster,
                    4.5,
                    NowUtc,
                    new RatingSummaryResult(
                        RatingTargetType.ParkItem,
                        "item-technical-id",
                        10,
                        4.2,
                        4.1)),
            });
        return ratings;
    }

    private static Mock<IUserRepository> CreateUserRepository(
        string? publicDisplayName = "Camille")
    {
        Mock<IUserRepository> users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByIdAsync(
                "owner-technical-id",
                CancellationToken.None))
            .ReturnsAsync(new User
            {
                Id = "owner-technical-id",
                Email = "private@example.com",
                IsActivated = true,
                PublicDisplayName = publicDisplayName,
                AvatarUrl = "/private/avatar-reference",
            });
        return users;
    }

    private static Mock<IImageRepository> CreateImageRepository()
    {
        Mock<IImageRepository> images = new Mock<IImageRepository>();
        images.Setup(value => value.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-technical-id",
                ImageCategory.Avatar,
                CancellationToken.None))
            .ReturnsAsync(new Image
            {
                Id = "avatar-public-id",
                OwnerType = ImageOwnerType.User,
                OwnerId = "owner-technical-id",
                Category = ImageCategory.Avatar,
                IsCurrent = true,
                IsPublished = true,
            });
        return images;
    }
}
