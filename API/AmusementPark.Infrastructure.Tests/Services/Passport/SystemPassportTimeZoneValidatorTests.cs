using AmusementPark.Infrastructure.Services.Passport;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Passport;

public sealed class SystemPassportTimeZoneValidatorTests
{
    [Theory]
    [InlineData("Europe/Paris")]
    [InlineData("UTC")]
    public void IsValid_WhenTimeZoneExists_ShouldAcceptIt(string timeZoneId)
    {
        SystemPassportTimeZoneValidator validator = new SystemPassportTimeZoneValidator();

        Assert.True(validator.IsValid(timeZoneId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Mars/Olympus")]
    public void IsValid_WhenTimeZoneDoesNotExist_ShouldRejectIt(string timeZoneId)
    {
        SystemPassportTimeZoneValidator validator = new SystemPassportTimeZoneValidator();

        Assert.False(validator.IsValid(timeZoneId));
    }
}
