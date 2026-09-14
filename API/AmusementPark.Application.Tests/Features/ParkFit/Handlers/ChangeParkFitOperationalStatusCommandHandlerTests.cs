using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
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
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        statuses.Setup(repository => repository.GetAsync(
                park.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkFitOperationalStatus?)null);
        statuses.Setup(repository => repository.ReplaceAsync(
                It.Is<ParkFitOperationalStatus>(status =>
                    status.State == ParkFitRecommendationState.Suspended
                    && status.Revision == 1),
                0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ParkFitOperationalStatusWriteOutcome.Success);
        ChangeParkFitOperationalStatusCommandHandler handler =
            new ChangeParkFitOperationalStatusCommandHandler(parks.Object, statuses.Object);

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
}
