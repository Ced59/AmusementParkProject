using AmusementPark.Application.Features.Seo.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class SeoSlugServiceTests
{
    [Theory]
    [InlineData(null, "item")]
    [InlineData("", "item")]
    [InlineData("   ", "item")]
    [InlineData("Parc Astérix", "parc-asterix")]
    [InlineData("  Phantasialand / Rookburgh!  ", "phantasialand-rookburgh")]
    [InlineData("Wodan Timbur Coaster", "wodan-timbur-coaster")]
    [InlineData("100% Fun", "100-fun")]
    public void ToSlug_WhenValueProvided_ShouldReturnExpectedSlug(string? value, string expected)
    {
        string result = SeoSlugService.ToSlug(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToSlug_WhenOnlySymbolsProvided_ShouldReturnFallback()
    {
        string result = SeoSlugService.ToSlug("!!!", "park");

        Assert.Equal("park", result);
    }
}
