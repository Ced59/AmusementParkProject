using AmusementPark.WebAPI.Configuration;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Configuration;

public sealed class RefreshTokenCookieSettingsTests
{
    [Theory]
    [InlineData("Strict", SameSiteMode.Strict)]
    [InlineData("strict", SameSiteMode.Strict)]
    [InlineData("Lax", SameSiteMode.Lax)]
    [InlineData("Unspecified", SameSiteMode.Unspecified)]
    [InlineData("None", SameSiteMode.None)]
    [InlineData("unknown", SameSiteMode.None)]
    [InlineData("", SameSiteMode.None)]
    public void GetSameSiteMode_WhenValueProvided_ShouldMapExpectedMode(string sameSite, SameSiteMode expected)
    {
        RefreshTokenCookieSettings settings = new RefreshTokenCookieSettings { SameSite = sameSite };

        SameSiteMode result = settings.GetSameSiteMode();

        Assert.Equal(expected, result);
    }
}
