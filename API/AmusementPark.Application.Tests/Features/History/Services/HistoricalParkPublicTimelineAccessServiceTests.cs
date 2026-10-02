using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalParkPublicTimelineAccessServiceTests
{
    [Fact]
    public async Task IsAvailableAsync_WhenCanonicalTimelineIsReadyWithoutIndexableYear_ShouldReturnTrue()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc historique",
            IsVisible = true,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalParkRolloutGateAssessmentService> gateAssessment =
            new Mock<IHistoricalParkRolloutGateAssessmentService>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoricalFact>());
        gateAssessment
            .Setup(service => service.AssessAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<IReadOnlyCollection<HistoricalFact>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoricalParkRolloutGate(2, 2, 1, Array.Empty<int>()));
        PublicParkHistoricalDataLoader loader = new PublicParkHistoricalDataLoader(
            parkRepository.Object,
            parkItemRepository.Object,
            parkZoneRepository.Object,
            factRepository.Object,
            gateAssessment.Object);
        HistoricalParkPublicTimelineAccessService service =
            new HistoricalParkPublicTimelineAccessService(loader);

        bool isAvailable = await service.IsAvailableAsync("park-1", CancellationToken.None);

        Assert.True(isAvailable);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        factRepository.VerifyAll();
        gateAssessment.VerifyAll();
    }
}
