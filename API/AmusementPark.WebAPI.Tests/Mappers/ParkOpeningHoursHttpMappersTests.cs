using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ParkOpeningHours;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class ParkOpeningHoursHttpMappersTests
{
    [Fact]
    public void ToDomainResult_WhenDaysOfWeekContainsInvalidValue_ShouldReturnValidationError()
    {
        ParkOpeningHoursScheduleDto dto = CreateValidSchedule();
        ParkOpeningHoursRuleDto rule = dto.RegularRules.Single();
        rule.DaysOfWeek = new[] { "Monday", "Mondya" };

        ApplicationResult<ParkOpeningHoursSchedule> result = dto.ToDomainResult("park-1");

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("park-opening-hours.invalid", error.Code);
        Assert.NotNull(error.Details);
        Assert.True(error.Details!.ContainsKey("regularRules[0].daysOfWeek"));
        Assert.Contains("Mondya", error.Details["regularRules[0].daysOfWeek"].Single());
    }

    [Fact]
    public void ToDomainResult_WhenLastAdmissionAtIsMalformed_ShouldReturnValidationError()
    {
        ParkOpeningHoursScheduleDto dto = CreateValidSchedule();
        ParkOpeningHoursTimeRangeDto timeRange = dto.RegularRules.Single().TimeRanges.Single();
        timeRange.LastAdmissionAt = "17h30";

        ApplicationResult<ParkOpeningHoursSchedule> result = dto.ToDomainResult("park-1");

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.NotNull(error.Details);
        Assert.True(error.Details!.ContainsKey("regularRules[0].timeRanges[0].lastAdmissionAt"));
        Assert.Contains("HH:mm", error.Details["regularRules[0].timeRanges[0].lastAdmissionAt"].Single());
    }

    [Fact]
    public void ToDomainResult_WhenMidnightTimesAreSubmitted_ShouldKeepValidTimes()
    {
        ParkOpeningHoursScheduleDto dto = CreateValidSchedule();
        ParkOpeningHoursTimeRangeDto timeRange = dto.RegularRules.Single().TimeRanges.Single();
        timeRange.OpensAt = "00:00";
        timeRange.ClosesAt = "02:00";
        timeRange.LastAdmissionAt = "01:30";

        ApplicationResult<ParkOpeningHoursSchedule> result = dto.ToDomainResult("park-1");

        Assert.True(result.IsSuccess);
        ParkOpeningHoursTimeRange mappedTimeRange = result.Value!.RegularRules.Single().TimeRanges.Single();
        Assert.Equal(new TimeOnly(0, 0), mappedTimeRange.OpensAt);
        Assert.Equal(new TimeOnly(2, 0), mappedTimeRange.ClosesAt);
        Assert.Equal(new TimeOnly(1, 30), mappedTimeRange.LastAdmissionAt);
    }

    private static ParkOpeningHoursScheduleDto CreateValidSchedule()
    {
        return new ParkOpeningHoursScheduleDto
        {
            ParkId = "park-1",
            TimeZoneId = "Europe/Paris",
            RegularRules = new[]
            {
                new ParkOpeningHoursRuleDto
                {
                    StartDate = "2026-07-01",
                    EndDate = "2026-07-31",
                    DaysOfWeek = new[] { "Monday", "Tuesday" },
                    IsClosed = false,
                    TimeRanges = new[]
                    {
                        new ParkOpeningHoursTimeRangeDto
                        {
                            OpensAt = "10:00",
                            ClosesAt = "18:00",
                            LastAdmissionAt = "17:30",
                        },
                    },
                },
            },
        };
    }
}
