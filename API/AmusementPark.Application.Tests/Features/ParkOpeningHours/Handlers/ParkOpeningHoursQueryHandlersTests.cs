using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Handlers;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Queries;
using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkOpeningHours.Handlers;

public sealed class ParkOpeningHoursQueryHandlersTests
{
    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    [InlineData(ParkStatus.Cancelled)]
    public async Task ScheduleQuery_WhenPublicParkIsNotOperating_ShouldNotExposeStoredOpeningHours(ParkStatus status)
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = status, IsVisible = true });
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        GetParkOpeningHoursScheduleQueryHandler handler = new GetParkOpeningHoursScheduleQueryHandler(
            parkRepository.Object,
            openingHoursRepository.Object);

        ApplicationResult<ParkOpeningHoursScheduleResult> result = await handler.HandleAsync(
            new GetParkOpeningHoursScheduleQuery("park-1", false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-opening-hours.not-found");
        openingHoursRepository.Verify(
            repository => repository.GetByParkIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        parkRepository.VerifyAll();
    }

    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    [InlineData(ParkStatus.Cancelled)]
    public async Task CalendarQuery_WhenPublicParkIsNotOperating_ShouldNotExposeStoredOpeningHours(ParkStatus status)
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = status, IsVisible = true });
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        GetParkOpeningHoursCalendarQueryHandler handler = new GetParkOpeningHoursCalendarQueryHandler(
            parkRepository.Object,
            openingHoursRepository.Object,
            new ParkOpeningHoursCalendarBuilder());

        ApplicationResult<ParkOpeningHoursCalendarResult> result = await handler.HandleAsync(
            new GetParkOpeningHoursCalendarQuery("park-1", null, null, false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-opening-hours.not-found");
        openingHoursRepository.Verify(
            repository => repository.GetByParkIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        parkRepository.VerifyAll();
    }
}
