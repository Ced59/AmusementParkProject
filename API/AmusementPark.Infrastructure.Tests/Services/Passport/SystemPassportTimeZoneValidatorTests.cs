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
    [InlineData("Pacific Standard Time")]
    public void IsValid_WhenTimeZoneIsNotAnExistingIanaIdentifier_ShouldRejectIt(string timeZoneId)
    {
        SystemPassportTimeZoneValidator validator = new SystemPassportTimeZoneValidator();

        Assert.False(validator.IsValid(timeZoneId));
    }
}
