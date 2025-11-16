using Services.Interfaces.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Services.Implementations.Images;

public class ImageCompressorService : IImageCompressorService
{
    private const int MaxFileSizeKb = 300;
    private const int MaxLongEdge = 1920; // plus grand côté max (ajuste selon ton besoin)

    public async Task<Dictionary<string, byte[]>> CompressAsync(Stream originalImageStream, string baseFileName)
    {
        if (originalImageStream.CanSeek)
        {
            originalImageStream.Position = 0;
        }

        using Image image = await Image.LoadAsync(originalImageStream);

        // On redimensionne en place si nécessaire
        ResizeInPlaceIfNeeded(image);

        (string Ext, Func<int, IImageEncoder> EncoderFactory)[] formats =
        {
            ("webp", q => new WebpEncoder { Quality = q }),
            ("jpg",  q => new JpegEncoder { Quality = q })
        };

        Dictionary<string, byte[]> result = new();

        foreach ((string ext, Func<int, IImageEncoder> encoderFactory) in formats)
        {
            byte[] compressed = await EncodeWithSizeLimitAsync(image, encoderFactory, MaxFileSizeKb);
            result.Add($"{baseFileName}.{ext}", compressed);
        }

        return result;
    }

    /// <summary>
    /// Redimensionne l'image en place si le plus grand côté dépasse MaxLongEdge.
    /// </summary>
    private static void ResizeInPlaceIfNeeded(Image image)
    {
        int longestEdge = Math.Max(image.Width, image.Height);

        if (longestEdge <= MaxLongEdge)
        {
            return;
        }

        float scale = (float)MaxLongEdge / longestEdge;
        int targetWidth = (int)(image.Width * scale);
        int targetHeight = (int)(image.Height * scale);

        image.Mutate(ctx =>
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, targetHeight),
                Mode = ResizeMode.Max
            }));
    }

    /// <summary>
    /// Encode l'image avec quelques qualités prédéfinies,
    /// en s'arrêtant dès que la taille est sous la limite.
    /// </summary>
    private static async Task<byte[]> EncodeWithSizeLimitAsync(
        Image image,
        Func<int, IImageEncoder> encoderFactory,
        int maxKb)
    {
        int[] qualities = { 80, 70, 60 }; // 3 tentatives max

        byte[]? lastAttempt = null;
        using MemoryStream ms = new();

        foreach (int quality in qualities)
        {
            ms.Position = 0;
            ms.SetLength(0);

            IImageEncoder encoder = encoderFactory(quality);

            await image.SaveAsync(ms, encoder);
            lastAttempt = ms.ToArray();

            if (ms.Length <= maxKb * 1024)
            {
                return lastAttempt;
            }
        }

        // Si aucune qualité n'atteint la limite, on renvoie la dernière tentative
        return lastAttempt ?? throw new InvalidOperationException("Échec de la compression de l'image.");
    }
}