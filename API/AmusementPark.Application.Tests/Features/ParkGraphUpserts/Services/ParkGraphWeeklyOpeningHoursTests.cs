using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphWeeklyOpeningHoursTests
{
    [Fact]
    public void WeeklyRules_RoundTripThroughNativeStandaloneExportWithoutDates()
    {
        using JsonDocument input = JsonDocument.Parse("""
            {"timeZoneId":"Europe/Lisbon","sourceUrl":"https://example.test/hours",
             "weeklyRules":[{"id":"usual","daysOfWeek":["Tuesday","Friday"],
             "timeRanges":[{"opensAt":"11:00","closesAt":"20:00"}]}]}
            """);
        List<string> errors = [];
        Assert.True(ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursScheduleData(input.RootElement));
        ParkOpeningHoursSchedule schedule = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadOpeningHoursSchedule(
            input.RootElement, "standalone-1", errors);
        Assert.Empty(errors);
        schedule = new ParkOpeningHoursScheduleNormalizer().Normalize(schedule).Value!;
        Assert.True(ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursData(schedule));

        object export = ExportParkGraphJsonQueryHandlerStandaloneAttractionExtensions.MapStandaloneOpeningHours(schedule);
        JsonElement exported = JsonSerializer.SerializeToElement(export, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("standalone-1", exported.GetProperty("standaloneAttractionId").GetString());
        JsonElement rule = exported.GetProperty("weeklyRules")[0];
        Assert.False(rule.TryGetProperty("startDate", out _));
        Assert.False(rule.TryGetProperty("endDate", out _));
        Assert.Empty(exported.GetProperty("regularRules").EnumerateArray());
        ParkOpeningHoursSchedule imported = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadOpeningHoursSchedule(
            exported, "standalone-1", errors);
        Assert.Empty(errors);
        Assert.Equal("usual", Assert.Single(imported.WeeklyRules).Id);
        Assert.Equal(schedule.WeeklyRules[0].DaysOfWeek, imported.WeeklyRules[0].DaysOfWeek);
        Assert.Equal(new TimeOnly(20, 0), imported.WeeklyRules[0].TimeRanges[0].ClosesAt);
        Assert.Empty(imported.RegularRules);
    }

    [Theory]
    [InlineData("{\"weeklyRules\":{}}")]
    [InlineData("{\"weeklyRules\":[{\"startDate\":\"2026-10-01\",\"daysOfWeek\":[\"Tuesday\"]}]}")]
    [InlineData("{\"weeklyRules\":[{\"endDate\":\"2026-10-01\",\"daysOfWeek\":[\"Tuesday\"]}]}")]
    [InlineData("{\"weeklyRules\":[{\"daysOfWeek\":[\"Tuesdayy\"]}]}")]
    public void WeeklyRules_RejectMalformedInputAndMisleadingDateBounds(string json)
    {
        using JsonDocument input = JsonDocument.Parse(json);
        List<string> errors = [];

        ParkGraphUpsertProcessorOpeningHoursExtensions.ReadOpeningHoursSchedule(input.RootElement, "standalone-1", errors);

        Assert.NotEmpty(errors);
    }
}
