using AmusementPark.Application.Features.Seo.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class SeoPageValuePolicyTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void IsImageGalleryIndexable_ShouldRequireAtLeastThreeImages(int imageCount, bool expected)
    {
        Assert.Equal(expected, SeoPageValuePolicy.IsImageGalleryIndexable(imageCount));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void IsCollectionIndexable_ShouldRequireAtLeastTwoEntries(int entryCount, bool expected)
    {
        Assert.Equal(expected, SeoPageValuePolicy.IsCollectionIndexable(entryCount));
    }
}
