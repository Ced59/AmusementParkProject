using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

public sealed class ChangeParkFitOperationalStatusCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldSuspendWithoutChangingParkVisibility()
    {
        Park park = new Park { Id = "park-1", Name = "Parc témoin", IsVisible = true };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkFitOperationalStatusRepository> statuses =
            new Mock<IParkFitOperationalStatusRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        statuses.Setup(repository => repository.GetAsync(
                park.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ParkFitOperationalStatus.CreateActive(park.Id));
        statuses.Setup(repository => repository.ReplaceAsync(
                It.Is<ParkFitOperationalStatus>(status =>
                    status.State == ParkFitRecommendationState.Suspended
                    && status.Revision == 1),
                0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ParkFitOperationalStatusWriteOutcome.Success);
        ChangeParkFitOperationalStatusCommandHandler handler =
            new ChangeParkFitOperationalStatusCommandHandler(
                parks.Object,
                items.Object,
                openingHours.Object,
                statuses.Object);

        ApplicationResult result = await handler.HandleAsync(
            new ChangeParkFitOperationalStatusCommand(
                park.Id,
                ParkFitRecommendationState.Suspended,
                "admin-1",
                "Preuve à revérifier",
                0));

        Assert.True(result.IsSuccess);
        Assert.True(park.IsVisible);
        parks.VerifyAll();
        statuses.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenQualityIsInsufficient_ShouldRejectActivation()
    {
        Park park = new Park { Id = "park-1", Name = "Parc incomplet", IsVisible = true };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        Mock<IParkFitOperationalStatusRepository> statuses =
            new Mock<IParkFitOperationalStatusRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        statuses.Setup(repository => repository.GetAsync(
                park.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkFitOperationalStatus?)null);
        items.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        openingHours.Setup(repository => repository.GetSummariesByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkOpeningHoursScheduleSummary>());
        ChangeParkFitOperationalStatusCommandHandler handler =
            new ChangeParkFitOperationalStatusCommandHandler(
                parks.Object,
                items.Object,
                openingHours.Object,
                statuses.Object);

        ApplicationResult result = await handler.HandleAsync(
            new ChangeParkFitOperationalStatusCommand(
                park.Id,
                ParkFitRecommendationState.Active,
                "admin-1",
                "Activation initiale",
                0));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "park-fit.operations.activation-quality-required");
        statuses.Verify(repository => repository.ReplaceAsync(
            It.IsAny<ParkFitOperationalStatus>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenQualityIsEligible_ShouldActivateThePark()
    {
        DateTime nowUtc = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
        Park park = BuildEligiblePark();
        ParkItem attraction = BuildEligibleAttraction(park.Id, nowUtc);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        Mock<IParkFitOperationalStatusRepository> statuses =
            new Mock<IParkFitOperationalStatusRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        statuses.Setup(repository => repository.GetAsync(
                park.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkFitOperationalStatus?)null);
        items.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { attraction });
        openingHours.Setup(repository => repository.GetSummariesByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkOpeningHoursScheduleSummary>
            {
                [park.Id] = new ParkOpeningHoursScheduleSummary
                {
                    ParkId = park.Id,
                    TimeZoneId = "UTC",
                    HasScheduleData = true,
                    LastDate = DateOnly.FromDateTime(nowUtc).AddDays(60),
                    CoverageSegments = new[]
                    {
                        new ParkOpeningHoursCoverageSegmentSummary
                        {
                            StartDate = DateOnly.FromDateTime(nowUtc),
                            EndDate = DateOnly.FromDateTime(nowUtc).AddDays(60),
                        },
                    },
                },
            });
        statuses.Setup(repository => repository.ReplaceAsync(
                It.Is<ParkFitOperationalStatus>(status =>
                    status.State == ParkFitRecommendationState.Active
                    && status.Decisions.Single().Type == ParkFitOperationalDecisionType.Activated),
                0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ParkFitOperationalStatusWriteOutcome.Success);
        ChangeParkFitOperationalStatusCommandHandler handler =
            new ChangeParkFitOperationalStatusCommandHandler(
                parks.Object,
                items.Object,
                openingHours.Object,
                statuses.Object,
                new ChangeParkFitOperationalStatusCommandHandlerTestsFixedTimeProvider(nowUtc));

        ApplicationResult result = await handler.HandleAsync(
            new ChangeParkFitOperationalStatusCommand(
                park.Id,
                ParkFitRecommendationState.Active,
                "admin-1",
                "Données validées",
                0));

        Assert.True(result.IsSuccess);
        statuses.VerifyAll();
    }

    private static Park BuildEligiblePark()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc éligible",
            CountryCode = "FR",
            IsVisible = true,
            Type = ParkType.ThemePark,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("fr", "Présentation publique du parc."),
            },
        };
        park.SetPosition(50, 3);
        return park;
    }

    private static ParkItem BuildEligibleAttraction(string parkId, DateTime nowUtc)
    {
        return new ParkItem
        {
            Id = "item-1",
            ParkId = parkId,
            Name = "Attraction témoin",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.FamilyRide,
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                IsIndoor = true,
                AccessConditions = new List<AttractionAccessCondition>
                {
                    new AttractionAccessCondition
                    {
                        Type = AttractionAccessConditionType.MinHeight,
                        Value = 100,
                        Unit = AttractionAccessConditionUnit.Centimeter,
                        SourceKind = AttractionAccessConditionSourceKind.Official,
                        SourceUrl = "https://example.test/access",
                        CollectedAtUtc = nowUtc.AddDays(-2),
                        VerifiedAtUtc = nowUtc.AddDays(-1),
                        SourceLanguageCode = "fr",
                        SourceSummary = new List<LocalizedText>
                        {
                            new LocalizedText("fr", "Taille minimale officielle."),
                        },
                        SourceConfidence = AttractionAccessConditionConfidence.High,
                        Scope = AttractionAccessConditionScope.Attraction,
                    },
                },
            },
        };
    }
}
