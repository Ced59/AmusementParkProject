using AmusementPark.Infrastructure.Time;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Time;

public sealed class SystemTripTimeZoneValidatorTests
{
    [Theory]
    [InlineData("Europe/Paris", true)]
    [InlineData("America/New_York", true)]
    [InlineData("Etc/UTC", true)]
    [InlineData("UTC", true)]
    [InlineData("Romance Standard Time", false)]
    [InlineData("Not/AZone", false)]
    public void IsValidIanaTimeZone_ShouldAcceptOnlyKnownIanaIdentifiers(string value, bool expected)
    {
        SystemTripTimeZoneValidator validator = new();

        Assert.Equal(expected, validator.IsValidIanaTimeZone(value));
    }

    [Fact]
    public void IsValidIanaTimeZone_WhenHostRecognizesZoneWithoutWindowsMapping_ShouldAcceptIt()
    {
        const string TimeZoneId = "Antarctica/Troll";
        Assert.False(TimeZoneInfo.TryConvertIanaIdToWindowsId(TimeZoneId, out string? _));
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(TimeZoneId, out TimeZoneInfo? _))
        {
            return;
        }

        SystemTripTimeZoneValidator validator = new();

        Assert.True(validator.IsValidIanaTimeZone(TimeZoneId));
    }
}
