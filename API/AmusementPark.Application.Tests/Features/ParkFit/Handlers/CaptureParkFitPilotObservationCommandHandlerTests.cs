using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

public sealed class CaptureParkFitPilotObservationCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPersistOnlyTheUtcDayAndValidatedObservation()
    {
        Mock<IParkFitPilotMetricsRepository> repository = new(MockBehavior.Strict);
        repository.Setup(value => value.IncrementAsync(
                new DateOnly(2026, 9, 14),
                It.Is<ParkFitPilotObservation>(observation =>
                    observation.EventKind == ParkFitPilotEventKind.SearchCompleted
                    && observation.ResultBand == ParkFitPilotResultBand.One),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        FixedParkFitPilotTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 9, 14, 23, 30, 0, TimeSpan.Zero));
        CaptureParkFitPilotObservationCommandHandler handler = new(
            repository.Object,
            timeProvider);

        ApplicationResult result = await handler.HandleAsync(new CaptureParkFitPilotObservationCommand(
            ParkFitPilotEventKind.SearchCompleted,
            ParkFitPilotResultBand.One,
            ParkFitPilotUnknownLevel.None,
            ParkFitPilotDurationBand.UnderHalfSecond,
            null,
            null,
            ParkFitScoreEvaluator.MethodVersion,
            [ParkFitDataQualityIssue.MissingOpeningCalendar]));

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }
}
