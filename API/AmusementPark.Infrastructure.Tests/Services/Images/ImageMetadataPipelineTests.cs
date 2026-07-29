using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Services.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Images;

public sealed class ImageMetadataPipelineTests
{
    [Fact]
    public async Task ExtractMetadataAsync_WhenGifIsAnimated_ShouldReportDetectedFormatAndAllFrames()
    {
        await using MemoryStream stream = new MemoryStream();
        using (Image<Rgba32> image = new Image<Rgba32>(2, 3))
        using (Image<Rgba32> secondFrameImage = new Image<Rgba32>(2, 3))
        {
            image.Frames.AddFrame(secondFrameImage.Frames.RootFrame);
            await image.SaveAsync(stream, new GifEncoder());
        }

        stream.Position = 0;
        ImageMetadataPipeline pipeline = new ImageMetadataPipeline();

        ImageProcessingMetadata? metadata = await pipeline.ExtractMetadataAsync(
            new ImageUploadRequest
            {
                Category = ImageCategory.Avatar,
                File = new FilePayload
                {
                    FileName = "avatar.jpg",
                    ContentType = "image/jpeg",
                    Length = stream.Length,
                    Content = stream,
                },
            },
            CancellationToken.None);

        Assert.NotNull(metadata);
        Assert.Equal("image/gif", metadata.DetectedContentType);
        Assert.Equal(2, metadata.FrameCount);
        Assert.Equal(2, metadata.Width);
        Assert.Equal(3, metadata.Height);
    }
}
