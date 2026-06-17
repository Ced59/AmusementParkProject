using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Configuration.Videos;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Videos;

public sealed class VideoThumbnailImporter : IVideoThumbnailImporter
{
    private static readonly HashSet<string> SupportedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IImageRepository imageRepository;
    private readonly IImageProcessingPipeline imageProcessingPipeline;
    private readonly IImageBinaryStorage imageBinaryStorage;
    private readonly VideoMetadataSettings settings;
    private readonly ILogger<VideoThumbnailImporter> logger;

    public VideoThumbnailImporter(
        IHttpClientFactory httpClientFactory,
        IImageRepository imageRepository,
        IImageProcessingPipeline imageProcessingPipeline,
        IImageBinaryStorage imageBinaryStorage,
        VideoMetadataSettings settings,
        ILogger<VideoThumbnailImporter> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.imageRepository = imageRepository;
        this.imageProcessingPipeline = imageProcessingPipeline;
        this.imageBinaryStorage = imageBinaryStorage;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<string?> ImportAsync(ResolvedVideoMetadata metadata, string videoId, CancellationToken cancellationToken)
    {
        if (metadata is null || string.IsNullOrWhiteSpace(videoId) || string.IsNullOrWhiteSpace(metadata.ThumbnailUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(metadata.ThumbnailUrl, UriKind.Absolute, out Uri? thumbnailUri) || thumbnailUri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        try
        {
            HttpClient client = this.httpClientFactory.CreateClient(ExternalVideoMetadataProvider.HttpClientName);
            using HttpResponseMessage response = await client.GetAsync(thumbnailUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            long? contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > this.settings.ThumbnailMaxBytes)
            {
                return null;
            }

            string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            if (!SupportedContentTypes.Contains(contentType))
            {
                return null;
            }

            await using MemoryStream content = new MemoryStream();
            await response.Content.CopyToAsync(content, cancellationToken);
            if (content.Length <= 0 || content.Length > this.settings.ThumbnailMaxBytes)
            {
                return null;
            }

            content.Position = 0;
            string imageId = Guid.NewGuid().ToString("N");
            FilePayload file = new FilePayload
            {
                FileName = BuildFileName(thumbnailUri, contentType),
                ContentType = contentType,
                Length = content.Length,
                Content = content,
            };

            ImageUploadRequest request = new ImageUploadRequest
            {
                ImageId = imageId,
                Category = ImageCategory.VideoThumbnail,
                File = file,
                Description = metadata.Title,
                WithWatermark = false,
                OwnerType = ImageOwnerType.Video,
                OwnerId = videoId.Trim(),
                StoragePath = $"video_thumbnail/{videoId.Trim()}/{imageId}",
            };

            ImageProcessingMetadata? imageMetadata = await this.imageProcessingPipeline.ExtractMetadataAsync(request, cancellationToken);
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            await this.imageBinaryStorage.SaveAsync(request.StoragePath, file, false, cancellationToken);
            ImageUploadRequest preparedRequest = new ImageUploadRequest
            {
                ImageId = imageId,
                Category = request.Category,
                File = file,
                Description = request.Description,
                WithWatermark = request.WithWatermark,
                OwnerType = request.OwnerType,
                OwnerId = request.OwnerId,
                StoragePath = request.StoragePath,
                Width = imageMetadata?.Width ?? 0,
                Height = imageMetadata?.Height ?? 0,
                SizeInBytes = imageMetadata?.SizeInBytes ?? file.Length,
            };

            Image image = await this.imageRepository.CreateAsync(preparedRequest, cancellationToken);
            return image.Id;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "Video thumbnail import failed for video {VideoId}.", videoId);
            return null;
        }
    }

    private static string BuildFileName(Uri thumbnailUri, string contentType)
    {
        string fileName = Path.GetFileName(thumbnailUri.AbsolutePath);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return fileName;
        }

        string extension = contentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg",
        };

        return $"thumbnail{extension}";
    }
}
