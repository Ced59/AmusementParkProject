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
    private const string WatermarkText = "amusement-park.fun";
    private const int MaxFileSizeKb = 300;
    private const int MaxLongEdge = 1920;

    private readonly IMinioClient minioClient;
    private readonly MinioImageStorageSettings settings;
    private readonly ILogger<MinioImageBinaryStorage> logger;
    private readonly Font watermarkFont;

    public MinioImageBinaryStorage(
        IMinioClient minioClient,
        MinioImageStorageSettings settings,
        ILogger<MinioImageBinaryStorage> logger)
    {
        this.minioClient = minioClient;
        this.settings = settings;
        this.logger = logger;
        this.watermarkFont = ResolveWatermarkFont();
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

    public async Task<(Stream Stream, string ContentType)?> GetBestAsync(string pathWithoutExtension, string? acceptHeader, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pathWithoutExtension))
        {
            return null;
        }

        bool supportsWebp = !string.IsNullOrWhiteSpace(acceptHeader) &&
                            acceptHeader.Contains("image/webp", StringComparison.OrdinalIgnoreCase);

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

    public async Task DeleteAsync(string pathWithoutExtension, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pathWithoutExtension))
        {
            return;
        }

        foreach (string objectName in new[]
                 {
                     $"{pathWithoutExtension}.webp",
                     $"{pathWithoutExtension}.jpg",
                     $"{pathWithoutExtension}.jpeg",
                     $"{pathWithoutExtension}.png",
                 })
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

    private static IEnumerable<(string extension, Func<int, IImageEncoder> encoderFactory, string contentType)> GetFormats()
    {
        yield return ("webp", quality => new WebpEncoder { Quality = quality }, "image/webp");
        yield return ("jpg", quality => new JpegEncoder { Quality = quality }, "image/jpeg");
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
        int[] qualities = { 80, 70, 60 };
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

    private static Font ResolveWatermarkFont()
    {
        FontCollection collection = new FontCollection();
        collection.AddSystemFonts();

        if (collection.TryGet("Arial", out FontFamily family))
        {
            return family.CreateFont(24f);
        }

        FontFamily fallbackFamily = SystemFonts.Families.First();
        return fallbackFamily.CreateFont(24f);
    }
}
