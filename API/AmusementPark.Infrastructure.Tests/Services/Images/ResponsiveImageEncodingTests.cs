using AmusementPark.Infrastructure.Services.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Images;

public sealed class ResponsiveImageEncodingTests
{
    [Theory]
    [InlineData(128, WebpFileFormatType.Lossy)]
    [InlineData(320, WebpFileFormatType.Lossless)]
    public async Task ResponsiveWebpEncoding_ShouldApplySmallVariantCompressionAndPreserveAlpha(
        int requestedWidth,
        WebpFileFormatType expectedFormat)
    {
        using Image<Rgba32> original = new Image<Rgba32>(128, 128);
        Random random = new Random(1729);
        for (int y = 0; y < original.Height; y++)
        {
            for (int x = 0; x < original.Width; x++)
            {
                byte alpha = x < 4 ? (byte)0 : x < 8 ? (byte)128 : (byte)255;
                original[x, y] = new Rgba32(
                    (byte)random.Next(256),
                    (byte)random.Next(256),
                    (byte)random.Next(256),
                    alpha);
            }
        }

        using MemoryStream losslessStream = new MemoryStream();
        await original.SaveAsWebpAsync(losslessStream, new WebpEncoder
        {
            FileFormat = WebpFileFormatType.Lossless,
        });
        losslessStream.Position = 0;
        using Image<Rgba32> decodedSource = await Image.LoadAsync<Rgba32>(losslessStream);
        Assert.Equal(WebpFileFormatType.Lossless, decodedSource.Metadata.GetWebpMetadata().FileFormat);

        using MemoryStream output = new MemoryStream();
        await decodedSource.SaveAsWebpAsync(
            output,
            MinioImageBinaryStorage.CreateResponsiveWebpEncoder(requestedWidth, 72));
        output.Position = 0;
        using Image<Rgba32> decodedOutput = await Image.LoadAsync<Rgba32>(output);

        Assert.Equal(expectedFormat, decodedOutput.Metadata.GetWebpMetadata().FileFormat);
        Assert.Equal(decodedSource.Size, decodedOutput.Size);
        Assert.Equal((byte)0, decodedOutput[0, 0].A);
        Assert.Equal((byte)128, decodedOutput[6, 6].A);
        Assert.Equal((byte)255, decodedOutput[64, 64].A);
        if (expectedFormat == WebpFileFormatType.Lossy)
        {
            Assert.True(output.Length < losslessStream.Length);
        }
    }

    [Theory]
    [InlineData(149, 144, 192, true, 149, 144)]
    [InlineData(128, 128, 128, true, 128, 128)]
    [InlineData(149, 144, 320, false, 149, 144)]
    [InlineData(512, 256, 128, true, 128, 64)]
    [InlineData(512, 256, 320, true, 320, 160)]
    public void PrepareResponsiveVariant_ShouldPreserveAspectRatioAndAvoidUpscaling(
        int originalWidth,
        int originalHeight,
        int requestedWidth,
        bool expectedResult,
        int expectedWidth,
        int expectedHeight)
    {
        using Image<Rgba32> image = new Image<Rgba32>(originalWidth, originalHeight);

        bool result = MinioImageBinaryStorage.PrepareResponsiveVariant(image, requestedWidth);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedWidth, image.Width);
        Assert.Equal(expectedHeight, image.Height);
    }
}
