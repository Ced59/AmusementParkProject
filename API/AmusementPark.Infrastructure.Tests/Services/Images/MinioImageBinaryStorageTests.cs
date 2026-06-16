using AmusementPark.Infrastructure.Services.Images;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Images;

public sealed class MinioImageBinaryStorageTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(1, 320)]
    [InlineData(320, 320)]
    [InlineData(321, 480)]
    [InlineData(480, 480)]
    [InlineData(481, 640)]
    [InlineData(640, 640)]
    [InlineData(641, 800)]
    [InlineData(800, 800)]
    [InlineData(801, 960)]
    [InlineData(960, 960)]
    [InlineData(961, 1280)]
    [InlineData(1280, 1280)]
    [InlineData(1281, 1600)]
    [InlineData(1600, 1600)]
    [InlineData(1601, 1920)]
    [InlineData(1920, 1920)]
    [InlineData(3000, 1920)]
    public void NormalizeResponsiveWidth_ShouldClampToSupportedVariant(int? requestedWidth, int? expectedWidth)
    {
        int? result = MinioImageBinaryStorage.NormalizeResponsiveWidth(requestedWidth);

        Assert.Equal(expectedWidth, result);
    }

    [Fact]
    public void GetObjectNamesForDeletion_ShouldIncludeOriginalAndResponsiveVariants()
    {
        string[] objectNames = MinioImageBinaryStorage.GetObjectNamesForDeletion("images/photo-1").ToArray();

        Assert.Contains("images/photo-1.webp", objectNames);
        Assert.Contains("images/photo-1.jpg", objectNames);
        Assert.Contains("images/photo-1.jpeg", objectNames);
        Assert.Contains("images/photo-1.png", objectNames);
        Assert.Contains("images/photo-1.w320.v2.webp", objectNames);
        Assert.Contains("images/photo-1.w320.v2.jpg", objectNames);
        Assert.Contains("images/photo-1.w1600.v2.webp", objectNames);
        Assert.Contains("images/photo-1.w1600.v2.jpg", objectNames);
        Assert.Contains("images/photo-1.w320.webp", objectNames);
        Assert.Contains("images/photo-1.w320.jpg", objectNames);
        Assert.Contains("images/photo-1.w1920.webp", objectNames);
        Assert.Contains("images/photo-1.w1920.jpg", objectNames);
        Assert.DoesNotContain("images/photo-1.w321.webp", objectNames);
        Assert.Equal(objectNames.Length, objectNames.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void GetResponsiveVariantObjectName_ShouldIncludeCurrentVariantVersion()
    {
        string objectName = MinioImageBinaryStorage.GetResponsiveVariantObjectName("images/photo-1", 960, "webp");

        Assert.Equal("images/photo-1.w960.v2.webp", objectName);
    }
}
