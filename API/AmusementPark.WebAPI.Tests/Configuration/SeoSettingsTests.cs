using AmusementPark.WebAPI.Configuration;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Configuration;

public sealed class SeoSettingsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetNormalizedPublicBaseUrl_WhenValueIsBlankAndHttpsNotRequired_ShouldReturnDefault(string? publicBaseUrl)
    {
        SeoSettings settings = new SeoSettings { PublicBaseUrl = publicBaseUrl! };

        string result = settings.GetNormalizedPublicBaseUrl(requireHttps: false);

        Assert.Equal("https://amusement-parks.fun", result);
    }

    [Fact]
    public void GetNormalizedPublicBaseUrl_WhenAbsoluteHttpUrlAndHttpsNotRequired_ShouldReturnOriginOnly()
    {
        SeoSettings settings = new SeoSettings { PublicBaseUrl = " http://localhost:4200/fr/path?query=1#hash " };

        string result = settings.GetNormalizedPublicBaseUrl(requireHttps: false);

        Assert.Equal("http://localhost:4200", result);
    }

    [Fact]
    public void GetNormalizedPublicBaseUrl_WhenInvalidUrlAndHttpsRequired_ShouldThrow()
    {
        SeoSettings settings = new SeoSettings { PublicBaseUrl = "not an url" };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => settings.GetNormalizedPublicBaseUrl(requireHttps: true));

        Assert.Contains("absolute HTTP(S)", exception.Message);
    }

    [Fact]
    public void GetNormalizedPublicBaseUrl_WhenHttpUrlAndHttpsRequired_ShouldThrow()
    {
        SeoSettings settings = new SeoSettings { PublicBaseUrl = "http://example.com" };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => settings.GetNormalizedPublicBaseUrl(requireHttps: true));

        Assert.Contains("HTTPS", exception.Message);
    }

    [Theory]
    [InlineData("https://localhost")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://[::1]")]
    public void GetNormalizedPublicBaseUrl_WhenLocalhostAndHttpsRequired_ShouldThrow(string publicBaseUrl)
    {
        SeoSettings settings = new SeoSettings { PublicBaseUrl = publicBaseUrl };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => settings.GetNormalizedPublicBaseUrl(requireHttps: true));

        Assert.Contains("localhost", exception.Message);
    }

    [Theory]
    [InlineData("https://example.com/path")]
    [InlineData("https://example.com/?query=1")]
    [InlineData("https://example.com/#hash")]
    public void GetNormalizedPublicBaseUrl_WhenPathQueryOrFragmentAndHttpsRequired_ShouldThrow(string publicBaseUrl)
    {
        SeoSettings settings = new SeoSettings { PublicBaseUrl = publicBaseUrl };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => settings.GetNormalizedPublicBaseUrl(requireHttps: true));

        Assert.Contains("root origin", exception.Message);
    }

    [Fact]
    public void GetNormalizedPublicBaseUrl_WhenValidProductionUrlProvided_ShouldReturnOriginWithoutTrailingSlash()
    {
        SeoSettings settings = new SeoSettings { PublicBaseUrl = "https://example.com/" };

        string result = settings.GetNormalizedPublicBaseUrl(requireHttps: true);

        Assert.Equal("https://example.com", result);
    }
}
