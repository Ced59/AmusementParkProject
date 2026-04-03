using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dtos.Images.Creating;
using Dtos.Images.Links;
using Entities.Model.Users;
using Microsoft.Extensions.Logging;
using OneOf;
using Services.Interfaces.Images;
using Services.Models.Images;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations.Images
{
    public class UserAvatarService : IUserAvatarService
    {
        private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif"
        };

        private readonly IHttpClientFactory httpClientFactory;
        private readonly ISavingImageService savingImageService;
        private readonly IImageLinksService imageLinksService;
        private readonly ILogger<UserAvatarService> logger;

        public UserAvatarService(
            IHttpClientFactory httpClientFactory,
            ISavingImageService savingImageService,
            IImageLinksService imageLinksService,
            ILogger<UserAvatarService> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.savingImageService = savingImageService;
            this.imageLinksService = imageLinksService;
            this.logger = logger;
        }

        public async Task<string> ImportExternalAvatarAsync(
            string imageUrl,
            string userId,
            ExternalLoginProvider provider,
            CancellationToken cancellationToken = default)
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
                HttpClient httpClient = httpClientFactory.CreateClient();
                using HttpResponseMessage response = await httpClient.GetAsync(
                    imageUri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Téléchargement de l'avatar externe impossible. Provider={Provider}, UserId={UserId}, StatusCode={StatusCode}",
                        provider,
                        userId,
                        response.StatusCode);

                    return string.Empty;
                }

                string? contentType = response.Content.Headers.ContentType?.MediaType;
                if (string.IsNullOrWhiteSpace(contentType) || !SupportedContentTypes.Contains(contentType))
                {
                    logger.LogWarning(
                        "Type de contenu non supporté pour l'avatar externe. Provider={Provider}, UserId={UserId}, ContentType={ContentType}",
                        provider,
                        userId,
                        contentType);

                    return string.Empty;
                }

                await using Stream remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using MemoryStream bufferedStream = new();
                await remoteStream.CopyToAsync(bufferedStream, cancellationToken);

                if (bufferedStream.Length == 0)
                {
                    return string.Empty;
                }

                bufferedStream.Position = 0;

                ImageSaveRequest request = new()
                {
                    FileStream = bufferedStream,
                    Category = ImageCategoryDto.AVATAR,
                    OriginalFileName = BuildAvatarFileName(provider, imageUri),
                    ContentType = contentType,
                    Description = $"Imported from {provider}",
                    WithWatermark = false
                };

                OneOf<ImageCreatedDto, ErrorDetail> saveResult = await savingImageService.SaveAsync(request);
                if (!saveResult.IsT0 || string.IsNullOrWhiteSpace(saveResult.AsT0.Id))
                {
                    logger.LogWarning(
                        "Échec de sauvegarde de l'avatar externe. Provider={Provider}, UserId={UserId}",
                        provider,
                        userId);

                    return string.Empty;
                }

                LinkImageToOwnerDto linkRequest = new()
                {
                    ImageId = saveResult.AsT0.Id,
                    OwnerType = ImageOwnerTypeDto.USER,
                    OwnerId = userId,
                    Description = request.Description,
                    SetAsCurrent = true
                };

                OneOf<Dtos.Images.ImageDto, ErrorDetail> linkResult = await imageLinksService.LinkImageAsync(linkRequest);
                if (!linkResult.IsT0)
                {
                    logger.LogWarning(
                        "Échec de rattachement de l'avatar externe. Provider={Provider}, UserId={UserId}",
                        provider,
                        userId);

                    return string.Empty;
                }

                return BuildAvatarUrl(linkResult.AsT0.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors de l'import de l'avatar externe pour l'utilisateur {UserId}", userId);
                return string.Empty;
            }
        }

        private static string BuildAvatarFileName(ExternalLoginProvider provider, Uri imageUri)
        {
            string extension = Path.GetExtension(imageUri.AbsolutePath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            return $"{provider.ToString().ToLowerInvariant()}-avatar{extension}";
        }

        private static string BuildAvatarUrl(string imageId)
        {
            return $"/images/{imageId}";
        }
    }
}
