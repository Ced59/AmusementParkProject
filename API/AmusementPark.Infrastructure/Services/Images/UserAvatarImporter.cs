using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using Microsoft.Extensions.Logging;
using AmusementPark.Application.Ports;

namespace AmusementPark.Infrastructure.Services.Images;

/// <summary>
/// Import technique d'avatars distants dans le pipeline images existant.
/// </summary>
public sealed class UserAvatarImporter : IUserAvatarImporter
{
    private const long MaximumAvatarFileSizeInBytes = 5 * 1024 * 1024;
    private const int MaximumAvatarEdge = 4096;
    private const long MaximumAvatarPixels = 8_000_000;
    private static readonly HashSet<string> SupportedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    };

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IImageProcessingPipeline imageProcessingPipeline;
    private readonly IImageBinaryStorage imageBinaryStorage;
    private readonly IImageRepository imageRepository;
    private readonly ILogger<UserAvatarImporter> logger;

    public UserAvatarImporter(
        IHttpClientFactory httpClientFactory,
        IImageProcessingPipeline imageProcessingPipeline,
        IImageBinaryStorage imageBinaryStorage,
        IImageRepository imageRepository,
        ILogger<UserAvatarImporter> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.imageProcessingPipeline = imageProcessingPipeline;
        this.imageBinaryStorage = imageBinaryStorage;
        this.imageRepository = imageRepository;
        this.logger = logger;
    }

    public async Task<string> DownloadAndSaveAsync(string imageUrl, string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(userId))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? imageUri)
            || (imageUri.Scheme != Uri.UriSchemeHttps && imageUri.Scheme != Uri.UriSchemeHttp))
        {
            return string.Empty;
        }

        try
        {
            HttpClient httpClient = this.httpClientFactory.CreateClient();
            using HttpResponseMessage response = await httpClient.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning("External avatar download failed for user {UserId} with status {StatusCode}.", userId, response.StatusCode);
                return string.Empty;
            }

            string? contentType = response.Content.Headers.ContentType?.MediaType;
            if (string.IsNullOrWhiteSpace(contentType) || !SupportedContentTypes.Contains(contentType))
            {
                this.logger.LogWarning("Unsupported avatar content type {ContentType} for user {UserId}.", contentType, userId);
                return string.Empty;
            }

            if (response.Content.Headers.ContentLength is long contentLength
                && contentLength > MaximumAvatarFileSizeInBytes)
            {
                this.logger.LogWarning("External avatar is too large for user {UserId}.", userId);
                return string.Empty;
            }

            await using Stream remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using MemoryStream bufferedStream = new MemoryStream();
            byte[] buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await remoteStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (bufferedStream.Length + bytesRead > MaximumAvatarFileSizeInBytes)
                {
                    this.logger.LogWarning("External avatar exceeded the size limit for user {UserId}.", userId);
                    return string.Empty;
                }

                await bufferedStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            bufferedStream.Position = 0;

            if (bufferedStream.Length == 0)
            {
                return string.Empty;
            }

            FilePayload filePayload = new FilePayload
            {
                FileName = BuildAvatarFileName(imageUri),
                ContentType = contentType,
                Length = bufferedStream.Length,
                Content = bufferedStream,
            };

            ImageUploadRequest baseRequest = new ImageUploadRequest
            {
                Category = ImageCategory.Avatar,
                File = filePayload,
                Description = "Imported external avatar",
                WithWatermark = false,
                OwnerType = ImageOwnerType.User,
                OwnerId = userId,
            };

            ImageProcessingMetadata? metadata = await this.imageProcessingPipeline.ExtractMetadataAsync(baseRequest, cancellationToken);
            if (metadata is null
                || metadata.Width <= 0
                || metadata.Height <= 0
                || metadata.Width > MaximumAvatarEdge
                || metadata.Height > MaximumAvatarEdge
                || (long)metadata.Width * metadata.Height > MaximumAvatarPixels)
            {
                this.logger.LogWarning("External avatar dimensions are invalid for user {UserId}.", userId);
                return string.Empty;
            }

            ImageUploadRequest request = new ImageUploadRequest
            {
                Category = baseRequest.Category,
                File = filePayload,
                Description = baseRequest.Description,
                WithWatermark = baseRequest.WithWatermark,
                OwnerType = baseRequest.OwnerType,
                OwnerId = baseRequest.OwnerId,
                Width = metadata?.Width ?? 0,
                Height = metadata?.Height ?? 0,
                SizeInBytes = metadata?.SizeInBytes ?? filePayload.Length,
                GeoLocation = null,
                ExifMetadata = null,
            };

            Image image = await this.imageRepository.CreateAsync(request, cancellationToken);
            if (string.IsNullOrWhiteSpace(image.Path))
            {
                return string.Empty;
            }

            if (filePayload.Content.CanSeek)
            {
                filePayload.Content.Position = 0;
            }

            await this.imageBinaryStorage.SaveWithoutMetadataAsync(
                image.Path,
                filePayload,
                false,
                cancellationToken);
            await this.imageRepository.SetCurrentAsync(image.Id, ImageOwnerType.User, userId, cancellationToken);
            return $"/images/{image.Id}";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error while importing external avatar for user {UserId}.", userId);
            return string.Empty;
        }
    }

    private static string BuildAvatarFileName(Uri imageUri)
    {
        string extension = Path.GetExtension(imageUri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        return $"external-avatar{extension}";
    }
}
