using AmusementPark.Infrastructure.Services.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Images;

public sealed class MinioImageBinaryStorageTests
{
    [Fact]
    public async Task LoadForStorageAsync_WhenPrivateImageIsAnimated_ShouldDecodeOnlyFirstFrame()
    {
        await using MemoryStream stream = new MemoryStream();
        using (Image<Rgba32> image = new Image<Rgba32>(2, 3))
        using (Image<Rgba32> secondFrameImage = new Image<Rgba32>(2, 3))
        using (Image<Rgba32> thirdFrameImage = new Image<Rgba32>(2, 3))
        {
            image.Frames.AddFrame(secondFrameImage.Frames.RootFrame);
            image.Frames.AddFrame(thirdFrameImage.Frames.RootFrame);
            await image.SaveAsync(stream, new GifEncoder());
        }

        stream.Position = 0;
        using Image decoded = await MinioImageBinaryStorage.LoadForStorageAsync(
            stream,
            stripMetadata: true,
            CancellationToken.None);

        Assert.Single(decoded.Frames);
    }

    [Fact]
    public void StripEmbeddedMetadata_ShouldRemoveExifProfiles()
    {
        using Image<Rgba32> image = new Image<Rgba32>(1, 1);
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitudeRef, "N");

        MinioImageBinaryStorage.StripEmbeddedMetadata(image);

        Assert.Null(image.Metadata.ExifProfile);
        Assert.Null(image.Metadata.IccProfile);
        Assert.Null(image.Metadata.IptcProfile);
        Assert.Null(image.Metadata.XmpProfile);
    }

    [Fact]
    public void AutoOrientAndStripEmbeddedMetadata_ShouldPreserveExifOrientationInPixels()
    {
        using Image<Rgba32> image = new Image<Rgba32>(2, 1);
        Rgba32 red = new Rgba32(255, 0, 0);
        Rgba32 blue = new Rgba32(0, 0, 255);
        image[0, 0] = red;
        image[1, 0] = blue;
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)6);

        MinioImageBinaryStorage.AutoOrientAndStripEmbeddedMetadata(image);

        Assert.Equal(1, image.Width);
        Assert.Equal(2, image.Height);
        Assert.Equal(red, image[0, 0]);
        Assert.Equal(blue, image[0, 1]);
        Assert.Null(image.Metadata.ExifProfile);
    }

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
