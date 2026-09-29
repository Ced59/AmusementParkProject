using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Services;

public sealed class PublicLiveLatestReaderTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 12, 0, 17, DateTimeKind.Utc);

    [Fact]
    public async Task ReadParkAsync_WhenPublicReadIsDisabled_ShouldReturnNotFoundWithoutReadingData()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ILiveLatestObservationRepository> observations =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        Mock<ILiveDataSourceCatalog> catalog = new Mock<ILiveDataSourceCatalog>(MockBehavior.Strict);
        catalog.SetupGet(value => value.IsPublicReadEnabled).Returns(false);
        PublicLiveLatestReader reader = CreateReader(parks, items, observations, catalog);

        AmusementPark.Application.Errors.ApplicationResult<PublicLiveTargetResult> result =
            await reader.ReadParkAsync("park-1", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.public-read.disabled", Assert.Single(result.Errors).Code);
        parks.VerifyNoOtherCalls();
        items.VerifyNoOtherCalls();
        observations.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(null)]
    public async Task ReadParkItemAsync_ShouldPreserveWaitValueAndSourceAttribution(
        int? waitTimeMinutes)
    {
        LiveLatestObservation observation = CreateObservation(
            NowUtc.AddMinutes(-2),
            waitTimeMinutes);
        PublicLiveLatestReader reader = CreateReader(observation);

        AmusementPark.Application.Errors.ApplicationResult<PublicLiveTargetResult> result =
            await reader.ReadParkItemAsync("item-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        PublicLiveTargetResult value = Assert.IsType<PublicLiveTargetResult>(result.Value);
        Assert.Equal(PublicLiveAvailability.Current, value.Availability);
        Assert.Equal(waitTimeMinutes, Assert.Single(value.Queues).WaitTimeMinutes);
        Assert.Equal(120, value.AgeSeconds);
        Assert.Equal("Powered by ThemeParks.wiki", value.Source?.AttributionText);
        Assert.Equal(NowUtc, value.AsOfUtc);
        Assert.Equal(NowUtc.AddMinutes(8), value.FreshnessTransitionAtUtc);
    }

    [Fact]
    public async Task ReadParkItemAsync_WhenObservationIsMissing_ShouldReturnExplicitAbsence()
    {
        PublicLiveLatestReader reader = CreateReader();

        AmusementPark.Application.Errors.ApplicationResult<PublicLiveTargetResult> result =
            await reader.ReadParkItemAsync("item-1", CancellationToken.None);

        PublicLiveTargetResult value = Assert.IsType<PublicLiveTargetResult>(result.Value);
        Assert.Equal(PublicLiveAvailability.NoObservation, value.Availability);
        Assert.Null(value.Status);
        Assert.Empty(value.Queues);
        Assert.Null(value.Source);
        Assert.Null(value.AgeSeconds);
    }

    [Fact]
    public async Task ReadParkItemAsync_ShouldHideExpiredOperationalFacts()
    {
        LiveLatestObservation observation = CreateObservation(NowUtc.AddHours(-1), 35);
        PublicLiveLatestReader reader = CreateReader(observation);

        AmusementPark.Application.Errors.ApplicationResult<PublicLiveTargetResult> result =
            await reader.ReadParkItemAsync("item-1", CancellationToken.None);

        PublicLiveTargetResult value = Assert.IsType<PublicLiveTargetResult>(result.Value);
        Assert.Equal(PublicLiveAvailability.Expired, value.Availability);
        Assert.Null(value.Status);
        Assert.Empty(value.Queues);
        Assert.Equal(LiveFreshnessState.Expired, value.Freshness);
        Assert.NotNull(value.Source);
    }

    [Fact]
    public async Task ReadParkItemAsync_WhenObservationExpiredInsideFormerBucket_ShouldHideFacts()
    {
        LiveLatestObservation observation = CreateObservation(
            new DateTime(2026, 9, 29, 11, 30, 1, DateTimeKind.Utc),
            35);
        PublicLiveLatestReader reader = CreateReader(observation);

        AmusementPark.Application.Errors.ApplicationResult<PublicLiveTargetResult> result =
            await reader.ReadParkItemAsync("item-1", CancellationToken.None);

        PublicLiveTargetResult value = Assert.IsType<PublicLiveTargetResult>(result.Value);
        Assert.Equal(NowUtc, value.AsOfUtc);
        Assert.Equal(PublicLiveAvailability.Expired, value.Availability);
        Assert.Null(value.Status);
        Assert.Empty(value.Queues);
    }

    [Fact]
    public async Task ReadParkItemAsync_ShouldNotExposeItemFromHiddenParentPark()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ILiveLatestObservationRepository> observations =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        items.Setup(repository => repository.GetByIdAsync("item-1", false, CancellationToken.None))
            .ReturnsAsync(CreateItem());
        parks.Setup(repository => repository.GetByIdAsync("park-1", false, CancellationToken.None))
            .ReturnsAsync((Park?)null);
        PublicLiveLatestReader reader = CreateReader(parks, items, observations);

        AmusementPark.Application.Errors.ApplicationResult<PublicLiveTargetResult> result =
            await reader.ReadParkItemAsync("item-1", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("parkitem.not-found", Assert.Single(result.Errors).Code);
        observations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReadParkItemsAsync_ShouldReadObservationsOnlyForVisibleItems()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ILiveLatestObservationRepository> observations =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync("park-1", false, CancellationToken.None))
            .ReturnsAsync(CreatePark());
        items.Setup(repository => repository.GetByParkIdAsync("park-1", false, CancellationToken.None))
            .ReturnsAsync(new[]
            {
                CreateItem("visible-2", "Beta"),
                CreateItem("visible-1", "Alpha"),
            });
        observations.Setup(repository => repository.GetParkItemsAsync(
                "park-1",
                It.Is<IReadOnlyCollection<string>>(targetIds =>
                    targetIds.SequenceEqual(new[] { "visible-2", "visible-1" })),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<LiveLatestObservation>());
        PublicLiveLatestReader reader = CreateReader(parks, items, observations);

        AmusementPark.Application.Errors.ApplicationResult<PublicParkLiveItemsResult> result =
            await reader.ReadParkItemsAsync("park-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        PublicParkLiveItemsResult value = Assert.IsType<PublicParkLiveItemsResult>(result.Value);
        Assert.Equal(new[] { "visible-1", "visible-2" }, value.Items.Select(static item => item.TargetId));
        Assert.All(
            value.Items,
            static item => Assert.Equal(PublicLiveAvailability.NoObservation, item.Availability));
        observations.VerifyAll();
    }

    private static PublicLiveLatestReader CreateReader(params LiveLatestObservation[] observationsToReturn)
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ILiveLatestObservationRepository> observations =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        items.Setup(repository => repository.GetByIdAsync("item-1", false, CancellationToken.None))
            .ReturnsAsync(CreateItem());
        parks.Setup(repository => repository.GetByIdAsync("park-1", false, CancellationToken.None))
            .ReturnsAsync(CreatePark());
        observations.Setup(repository => repository.GetByTargetAsync(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(observationsToReturn);
        return CreateReader(parks, items, observations);
    }

    private static PublicLiveLatestReader CreateReader(
        Mock<IParkRepository> parks,
        Mock<IParkItemRepository> items,
        Mock<ILiveLatestObservationRepository> observations,
        Mock<ILiveDataSourceCatalog>? sourceCatalog = null)
    {
        Mock<ILiveDataSourceCatalog> catalog = sourceCatalog
            ?? new Mock<ILiveDataSourceCatalog>(MockBehavior.Strict);
        if (sourceCatalog is null)
        {
            catalog.SetupGet(value => value.IsPublicReadEnabled).Returns(true);
        }
        catalog.Setup(value => value.Find(LiveDataSourceId.Parse("themeparks-wiki")))
            .Returns(CreatePresentation());
        Mock<TimeProvider> clock = new Mock<TimeProvider>(MockBehavior.Strict);
        clock.Setup(value => value.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        return new PublicLiveLatestReader(
            parks.Object,
            items.Object,
            observations.Object,
            catalog.Object,
            new PublicLiveTargetResultFactory(
                catalog.Object,
                new LiveLatestObservationSelectionPolicy()),
            clock.Object);
    }

    private static LiveLatestObservation CreateObservation(DateTime observedAtUtc, int? waitTimeMinutes)
    {
        return new LiveLatestObservation(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            LiveOperationalStatus.Open,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, waitTimeMinutes, false) },
            new LiveObservationProvenance(
                LiveDataSourceId.Parse("themeparks-wiki"),
                "external-1",
                observedAtUtc,
                observedAtUtc.AddSeconds(1),
                observedAtUtc.AddSeconds(2),
                "correlation",
                "adapter-1",
                "mapping-1",
                LiveDataConfidence.Medium,
                "usage-1",
                "transformation-1"),
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(1)),
            null);
    }

    private static LiveDataSourcePresentation CreatePresentation()
    {
        return new LiveDataSourcePresentation(
            new LiveDataSource(
                LiveDataSourceId.Parse("themeparks-wiki"),
                LiveDataSourceType.AuthorizedAggregator,
                "ThemeParks.wiki",
                new SourceUsagePolicy(
                    "usage-1",
                    "https://themeparks.wiki/terms",
                    true,
                    true,
                    false,
                    true,
                    "attribution",
                    NowUtc),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(30),
                LiveDataSourceStatus.Active),
            100,
            "Powered by ThemeParks.wiki",
            "https://themeparks.wiki/");
    }

    private static Park CreatePark()
    {
        return new Park { Id = "park-1", Name = "Park", IsVisible = true };
    }

    private static ParkItem CreateItem(
        string id = "item-1",
        string name = "Attraction")
    {
        return new ParkItem
        {
            Id = id,
            ParkId = "park-1",
            Name = name,
            IsVisible = true,
        };
    }
}
