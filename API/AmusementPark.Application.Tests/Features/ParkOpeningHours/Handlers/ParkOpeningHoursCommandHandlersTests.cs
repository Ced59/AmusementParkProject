using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Commands;
using AmusementPark.Application.Features.ParkOpeningHours.Handlers;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkOpeningHours.Handlers;

public sealed class ParkOpeningHoursCommandHandlersTests
{
    [Fact]
    public async Task HandleAsync_WhenScheduleIsSaved_ShouldRefreshSitemap()
    {
        ParkOpeningHoursSchedule schedule = CreateSchedule();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        openingHoursRepository
            .Setup(repository => repository.UpsertAsync(It.Is<ParkOpeningHoursSchedule>(candidate => candidate.ParkId == "park-1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOpeningHoursSchedule candidate, CancellationToken _) => candidate);
        sitemapRefreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UpsertParkOpeningHoursScheduleCommandHandler handler = new UpsertParkOpeningHoursScheduleCommandHandler(
            parkRepository.Object,
            openingHoursRepository.Object,
            new ParkOpeningHoursScheduleNormalizer(),
            new ParkOpeningHoursCoverageSegmentBuilder(),
            sitemapRefreshScheduler.Object);

        ApplicationResult<ParkOpeningHoursSchedule> result = await handler.HandleAsync(
            new UpsertParkOpeningHoursScheduleCommand(schedule),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        parkRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
        sitemapRefreshScheduler.VerifyAll();
    }

    private static ParkOpeningHoursSchedule CreateSchedule()
    {
        return new ParkOpeningHoursSchedule
        {
            ParkId = " park-1 ",
            TimeZoneId = "UTC",
            RegularRules = new List<ParkOpeningHoursRule>
            {
                new ParkOpeningHoursRule
                {
                    StartDate = new DateOnly(2026, 7, 1),
                    EndDate = new DateOnly(2026, 7, 31),
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday },
                    TimeRanges = new List<ParkOpeningHoursTimeRange>
                    {
                        new ParkOpeningHoursTimeRange
                        {
                            OpensAt = new TimeOnly(10, 0),
                            ClosesAt = new TimeOnly(18, 0),
                        },
                    },
                },
            },
        };
    }
}
