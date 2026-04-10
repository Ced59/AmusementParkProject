using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Images;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Images;

/// <summary>
/// Import technique d'avatars distants dans le pipeline images existant.
/// </summary>
public sealed class UserAvatarImporter : IUserAvatarImporter
{
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

            await using Stream remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            MemoryStream bufferedStream = new MemoryStream();
            await remoteStream.CopyToAsync(bufferedStream, cancellationToken);
            bufferedStream.Position = 0;

            if (bufferedStream.Length == 0)
            {
                bufferedStream.Dispose();
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
                GeoLocation = metadata?.GeoLocation,
                ExifMetadata = metadata?.ExifMetadata,
            };

            Image image = await this.imageRepository.CreateAsync(request, cancellationToken);
            if (string.IsNullOrWhiteSpace(image.Path))
            {
                bufferedStream.Dispose();
                return string.Empty;
            }

            if (filePayload.Content.CanSeek)
            {
                filePayload.Content.Position = 0;
            }

            await this.imageBinaryStorage.SaveAsync(image.Path, filePayload, false, cancellationToken);
            await this.imageRepository.SetCurrentAsync(image.Id, ImageOwnerType.User, userId, cancellationToken);
            bufferedStream.Dispose();
            return $"/images/{image.Id}";
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
