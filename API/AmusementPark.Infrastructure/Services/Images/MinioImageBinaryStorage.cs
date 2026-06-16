using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Infrastructure.Configuration.Images;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AmusementPark.Infrastructure.Services.Images;

/// <summary>
/// Stockage binaire MinIO des variantes d'images avec compression et watermark.
/// </summary>
public sealed class MinioImageBinaryStorage : IImageBinaryStorage
{
    private const string WatermarkText = "amusement-parks.fun";
    private const int MaxFileSizeKb = 300;
    private const int MaxLongEdge = 1920;
    private const int ResponsiveVariantVersion = 2;
    private static readonly int[] ResponsiveWidths = new[] { 320, 480, 640, 800, 960, 1280, 1600, 1920 };
    private static readonly int[] DefaultQualitySteps = new[] { 80, 70, 60 };
    private static readonly int[] ResponsiveQualitySteps = new[] { 72, 64, 56 };

    private readonly IMinioClient minioClient;
    private readonly MinioImageStorageSettings settings;
    private readonly ILogger<MinioImageBinaryStorage> logger;
    private readonly Font? watermarkFont;

    public MinioImageBinaryStorage(
        IMinioClient minioClient,
        MinioImageStorageSettings settings,
        ILogger<MinioImageBinaryStorage> logger)
    {
        this.minioClient = minioClient;
        this.settings = settings;
        this.logger = logger;
        this.watermarkFont = ResolveWatermarkFont(logger);
    }

    public async Task<IReadOnlyCollection<string>> SaveAsync(string pathWithoutExtension, FilePayload file, bool withWatermark, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathWithoutExtension);
        ArgumentNullException.ThrowIfNull(file);

        await EnsureBucketExistsAsync(cancellationToken);

        if (file.Content.CanSeek)
        {
            file.Content.Position = 0;
        }

        await using MemoryStream inputCopy = new MemoryStream();
        await file.Content.CopyToAsync(inputCopy, cancellationToken);
        inputCopy.Position = 0;

        await using MemoryStream workingStream = withWatermark
            ? await ApplyWatermarkAsync(inputCopy, WatermarkText, cancellationToken)
            : new MemoryStream(inputCopy.ToArray());

        workingStream.Position = 0;
        using Image image = await Image.LoadAsync(workingStream, cancellationToken);
        ResizeInPlaceIfNeeded(image);

        List<string> savedFiles = new List<string>();

        foreach ((string extension, Func<int, IImageEncoder> encoderFactory, string contentType) format in GetFormats())
        {
            byte[] content = await EncodeWithSizeLimitAsync(image, format.encoderFactory, cancellationToken);
            string objectName = $"{pathWithoutExtension}.{format.extension}";

            await using MemoryStream objectStream = new MemoryStream(content);
            await this.minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(this.settings.Bucket)
                    .WithObject(objectName)
                    .WithStreamData(objectStream)
                    .WithObjectSize(objectStream.Length)
                    .WithContentType(format.contentType));

            savedFiles.Add(objectName);
        }

        return savedFiles;
    }

    public async Task<(Stream Stream, string ContentType)?> GetBestAsync(string pathWithoutExtension, string? acceptHeader, int? width, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pathWithoutExtension))
        {
            return null;
        }

        bool supportsWebp = !string.IsNullOrWhiteSpace(acceptHeader) &&
                            acceptHeader.Contains("image/webp", StringComparison.OrdinalIgnoreCase);

        int? responsiveWidth = NormalizeResponsiveWidth(width);
        if (responsiveWidth is int requestedWidth)
        {
            (Stream Stream, string ContentType)? resized = await GetResizedVariantAsync(pathWithoutExtension, requestedWidth, supportsWebp, cancellationToken);
            if (resized is not null)
            {
                return resized;
            }
        }

        return await GetOriginalBestAsync(pathWithoutExtension, supportsWebp, cancellationToken);
    }

    private async Task<(Stream Stream, string ContentType)?> GetOriginalBestAsync(string pathWithoutExtension, bool supportsWebp, CancellationToken cancellationToken)
    {
        if (supportsWebp)
        {
            (Stream Stream, string ContentType)? webp = await TryGetObjectAsync($"{pathWithoutExtension}.webp", "image/webp", cancellationToken);
            if (webp is not null)
            {
                return webp;
            }
        }

        (Stream Stream, string ContentType)? jpg = await TryGetObjectAsync($"{pathWithoutExtension}.jpg", "image/jpeg", cancellationToken);
        if (jpg is not null)
        {
            return jpg;
        }

        (Stream Stream, string ContentType)? png = await TryGetObjectAsync($"{pathWithoutExtension}.png", "image/png", cancellationToken);
        if (png is not null)
        {
            return png;
        }

        return null;
    }

    private async Task<(Stream Stream, string ContentType)?> GetResizedVariantAsync(string pathWithoutExtension, int width, bool supportsWebp, CancellationToken cancellationToken)
    {
        foreach ((string extension, Func<int, IImageEncoder> encoderFactory, string contentType) format in GetReadableFormats(supportsWebp))
        {
            string objectName = GetResponsiveVariantObjectName(pathWithoutExtension, width, format.extension);
            (Stream Stream, string ContentType)? cached = await TryGetObjectAsync(objectName, format.contentType, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            (Stream Stream, string ContentType)? generated = await TryCreateResizedVariantAsync(
                pathWithoutExtension,
                objectName,
                width,
                format.encoderFactory,
                format.contentType,
                cancellationToken);

            if (generated is not null)
            {
                return generated;
            }
        }

        return null;
    }

    private async Task<(Stream Stream, string ContentType)?> TryCreateResizedVariantAsync(
        string pathWithoutExtension,
        string objectName,
        int width,
        Func<int, IImageEncoder> encoderFactory,
        string contentType,
        CancellationToken cancellationToken)
    {
        (Stream Stream, string ContentType)? source = await TryGetSourceObjectAsync(pathWithoutExtension, cancellationToken);
        if (source is null)
        {
            return null;
        }

        try
        {
            await using Stream sourceStream = source.Value.Stream;
            using Image image = await Image.LoadAsync(sourceStream, cancellationToken);

            if (image.Width <= width)
            {
                return null;
            }

            double scale = (double)width / image.Width;
            int targetHeight = Math.Max(1, (int)Math.Round(image.Height * scale));
            image.Mutate(context =>
            {
                context.Resize(new ResizeOptions
                {
                    Size = new Size(width, targetHeight),
                    Mode = ResizeMode.Max,
                });
            });

            byte[] content = await EncodeResponsiveVariantAsync(image, encoderFactory, cancellationToken);
            MemoryStream outputStream = new MemoryStream(content);
            await this.minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(this.settings.Bucket)
                    .WithObject(objectName)
                    .WithStreamData(outputStream)
                    .WithObjectSize(outputStream.Length)
                    .WithContentType(contentType),
                cancellationToken);

            outputStream.Position = 0;
            return (outputStream, contentType);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "Unable to create responsive image variant {ObjectName} in MinIO bucket {Bucket}.", objectName, this.settings.Bucket);
            return null;
        }
    }

    private async Task<(Stream Stream, string ContentType)?> TryGetSourceObjectAsync(string pathWithoutExtension, CancellationToken cancellationToken)
    {
        foreach ((string extension, string contentType) format in GetSourceFormats())
        {
            (Stream Stream, string ContentType)? source = await TryGetObjectAsync($"{pathWithoutExtension}.{format.extension}", format.contentType, cancellationToken);
            if (source is not null)
            {
                return source;
            }
        }

        return null;
    }

    private static IEnumerable<(string extension, string contentType)> GetSourceFormats()
    {
        yield return ("webp", "image/webp");
        yield return ("jpg", "image/jpeg");
        yield return ("jpeg", "image/jpeg");
        yield return ("png", "image/png");
    }

    public async Task DeleteAsync(string pathWithoutExtension, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pathWithoutExtension))
        {
            return;
        }

        foreach (string objectName in GetObjectNamesForDeletion(pathWithoutExtension))
        {
            try
            {
                await this.minioClient.RemoveObjectAsync(
                    new RemoveObjectArgs()
                        .WithBucket(this.settings.Bucket)
                        .WithObject(objectName));
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogWarning(exception, "Unable to delete image object {ObjectName} from MinIO bucket {Bucket}.", objectName, this.settings.Bucket);
            }
        }
    }

    internal static IEnumerable<string> GetObjectNamesForDeletion(string pathWithoutExtension)
    {
        yield return $"{pathWithoutExtension}.webp";
        yield return $"{pathWithoutExtension}.jpg";
        yield return $"{pathWithoutExtension}.jpeg";
        yield return $"{pathWithoutExtension}.png";

        foreach (int width in ResponsiveWidths)
        {
            yield return $"{pathWithoutExtension}.w{width}.v{ResponsiveVariantVersion}.webp";
            yield return $"{pathWithoutExtension}.w{width}.v{ResponsiveVariantVersion}.jpg";
            yield return $"{pathWithoutExtension}.w{width}.webp";
            yield return $"{pathWithoutExtension}.w{width}.jpg";
        }
    }

    internal static string GetResponsiveVariantObjectName(string pathWithoutExtension, int width, string extension)
    {
        return $"{pathWithoutExtension}.w{width}.v{ResponsiveVariantVersion}.{extension}";
    }

    private static IEnumerable<(string extension, Func<int, IImageEncoder> encoderFactory, string contentType)> GetFormats()
    {
        yield return ("webp", quality => new WebpEncoder { Quality = quality }, "image/webp");
        yield return ("jpg", quality => new JpegEncoder { Quality = quality }, "image/jpeg");
    }

    private static IEnumerable<(string extension, Func<int, IImageEncoder> encoderFactory, string contentType)> GetReadableFormats(bool supportsWebp)
    {
        if (supportsWebp)
        {
            yield return ("webp", quality => new WebpEncoder { Quality = quality }, "image/webp");
        }

        yield return ("jpg", quality => new JpegEncoder { Quality = quality }, "image/jpeg");
    }

    internal static int? NormalizeResponsiveWidth(int? width)
    {
        if (width is null || width <= 0)
        {
            return null;
        }

        foreach (int candidate in ResponsiveWidths)
        {
            if (width <= candidate)
            {
                return candidate;
            }
        }

        return ResponsiveWidths[^1];
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        bool exists = await this.minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(this.settings.Bucket));

        if (!exists)
        {
            await this.minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(this.settings.Bucket));
        }
    }

    private async Task<(Stream Stream, string ContentType)?> TryGetObjectAsync(string objectName, string contentType, CancellationToken cancellationToken)
    {
        try
        {
            await this.minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(this.settings.Bucket)
                    .WithObject(objectName),
                cancellationToken);

            MemoryStream stream = new MemoryStream();
            await this.minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(this.settings.Bucket)
                    .WithObject(objectName)
                    .WithCallbackStream(callbackStream => callbackStream.CopyTo(stream)),
                cancellationToken);

            stream.Position = 0;
            return (stream, contentType);
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return null;
        }
    }

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

        image.Mutate(context =>
        {
            context.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, targetHeight),
                Mode = ResizeMode.Max,
            });
        });
    }

    private static async Task<byte[]> EncodeWithSizeLimitAsync(Image image, Func<int, IImageEncoder> encoderFactory, CancellationToken cancellationToken)
    {
        return await EncodeWithQualitiesAsync(image, encoderFactory, DefaultQualitySteps, cancellationToken);
    }

    private static async Task<byte[]> EncodeResponsiveVariantAsync(Image image, Func<int, IImageEncoder> encoderFactory, CancellationToken cancellationToken)
    {
        return await EncodeWithQualitiesAsync(image, encoderFactory, ResponsiveQualitySteps, cancellationToken);
    }

    private static async Task<byte[]> EncodeWithQualitiesAsync(Image image, Func<int, IImageEncoder> encoderFactory, IReadOnlyCollection<int> qualities, CancellationToken cancellationToken)
    {
        byte[]? lastAttempt = null;

        using MemoryStream stream = new MemoryStream();
        foreach (int quality in qualities)
        {
            stream.Position = 0;
            stream.SetLength(0);

            IImageEncoder encoder = encoderFactory(quality);
            await image.SaveAsync(stream, encoder, cancellationToken);
            lastAttempt = stream.ToArray();

            if (stream.Length <= MaxFileSizeKb * 1024)
            {
                return lastAttempt;
            }
        }

        return lastAttempt ?? Array.Empty<byte>();
    }

    private async Task<MemoryStream> ApplyWatermarkAsync(Stream imageStream, string watermarkText, CancellationToken cancellationToken)
    {
        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        if (this.watermarkFont is null)
        {
            this.logger.LogWarning("Image watermark skipped because no usable font is available on the current host.");

            MemoryStream copy = new MemoryStream();
            await imageStream.CopyToAsync(copy, cancellationToken);
            copy.Position = 0;
            return copy;
        }

        using Image image = await Image.LoadAsync(imageStream, cancellationToken);
        int margin = 10;
        image.Mutate(context =>
        {
            RichTextOptions options = new RichTextOptions(this.watermarkFont)
            {
                Origin = new PointF(image.Width - margin, image.Height - margin),
                WrappingLength = image.Width,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
            };

            context.DrawText(options, watermarkText, Color.White);
        });

        MemoryStream output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 85 }, cancellationToken);
        output.Position = 0;
        return output;
    }

    private static Font? ResolveWatermarkFont(ILogger logger)
    {
        try
        {
            FontCollection collection = new FontCollection();
            collection.AddSystemFonts();

            if (collection.TryGet("Arial", out FontFamily arialFamily))
            {
                return arialFamily.CreateFont(24f);
            }

            if (collection.TryGet("DejaVu Sans", out FontFamily dejavuFamily))
            {
                return dejavuFamily.CreateFont(24f);
            }

            if (collection.Families.Any())
            {
                return collection.Families.First().CreateFont(24f);
            }

            if (SystemFonts.Families.Any())
            {
                return SystemFonts.Families.First().CreateFont(24f);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to resolve a system font for image watermarking.");
            return null;
        }

        logger.LogWarning("Unable to resolve a system font for image watermarking.");
        return null;
    }
}
