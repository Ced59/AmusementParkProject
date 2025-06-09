using Services.Interfaces.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace Services.Implementations.Images;

public class ImageCompressorService : IImageCompressorService
{
    private const int MaxFileSizeKb = 300;

    public async Task<Dictionary<string, byte[]>> CompressAsync(Stream originalImageStream, string baseFileName)
    {
        originalImageStream.Position = 0;

        using Image image = await Image.LoadAsync(originalImageStream);

        (string ext, Func<int, IImageEncoder> encoderFactory)[] formats = new (string ext, Func<int, IImageEncoder> encoderFactory)[]
        {
            ("webp", q => new WebpEncoder { Quality = q }),
            ("jpg",  q => new JpegEncoder { Quality = q })
        };

        Dictionary<string, byte[]> result = new();

        foreach ((string ext, Func<int, IImageEncoder> encoderFactory) in formats)
        {
            byte[] compressed = await CompressToMaxSize(image, encoderFactory, MaxFileSizeKb);
            result.Add($"{baseFileName}.{ext}", compressed);
        }

        return result;
    }

    private async Task<byte[]> CompressToMaxSize(Image image, Func<int, IImageEncoder> encoderFactory, int maxKb)
    {
        int width = image.Width;
        int height = image.Height;
        int quality = 85;

        for (int i = 0; i < 10; i++)
        {
            using MemoryStream ms = new();
            Image clone = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));

            IImageEncoder encoder = encoderFactory(quality);

            await clone.SaveAsync(ms, encoder);
            if (ms.Length <= maxKb * 1024)
                return ms.ToArray();

            width = (int)(width * 0.9);
            height = (int)(height * 0.9);
            quality -= 5;
        }

        throw new InvalidOperationException("Impossible de compresser l'image sous 300 Ko.");
    }
}