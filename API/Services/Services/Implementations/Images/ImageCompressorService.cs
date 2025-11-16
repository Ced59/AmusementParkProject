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

    private async Task<byte[]> CompressToMaxSize(Image original, Func<int, IImageEncoder> encoderFactory, int maxKb)
    {
        int quality = 85;
        int minQuality = 40;
        byte[]? lastValid = null;

        // Phase 1 – Compression par qualité uniquement
        for (; quality >= minQuality; quality -= 5)
        {
            using MemoryStream ms = new MemoryStream();
            await original.SaveAsync(ms, encoderFactory(quality));

            if (ms.Length <= maxKb * 1024)
                return ms.ToArray(); // Compression réussie

            lastValid = ms.ToArray(); // Stocke la meilleure tentative même si > max
        }

        // Phase 2 – Resize + qualité basse si toujours trop gros
        double scale = 0.9;
        int width = original.Width;
        int height = original.Height;

        for (int i = 0; i < 5; i++) // max 5 essais avec redimensionnement
        {
            width = (int)(width * scale);
            height = (int)(height * scale);

            using Image resized = original.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));

            using MemoryStream ms = new MemoryStream();
            await resized.SaveAsync(ms, encoderFactory(minQuality));

            if (ms.Length <= maxKb * 1024)
                return ms.ToArray();
        }

        // Dernier recours : retourne le meilleur résultat, même s’il dépasse la limite
        return lastValid ?? throw new InvalidOperationException("Échec de compression.");
    }
}