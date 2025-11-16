using Common.Extensions;
using Dtos.Images.Creating;
using Entities.Model.Errors;
using Entities.Model.Images;
using Microsoft.Extensions.Logging;
using OneOf;
using Services.Interfaces.Images;

namespace Services.Implementations.Images
{
    public class SavingImageService : ISavingImageService
    {
        private const string WatermarkText = "amusement-park.fun";

        private readonly IImageCompressorService imageCompressorService;
        private readonly IImageStorageService imageStorageService;
        private readonly IWaterMarkService waterMarkService;
        private readonly IImageMetadataExtractorService imageMetadataExtractorService;
        private readonly ILogger<SavingImageService> logger;

        public SavingImageService(
            IImageCompressorService imageCompressorService,
            IImageStorageService imageStorageService,
            IWaterMarkService waterMarkService,
            IImageMetadataExtractorService imageMetadataExtractorService,
            ILogger<SavingImageService> logger)
        {
            this.imageCompressorService = imageCompressorService;
            this.imageStorageService = imageStorageService;
            this.waterMarkService = waterMarkService;
            this.imageMetadataExtractorService = imageMetadataExtractorService;
            this.logger = logger;
        }

        public async Task<OneOf<ImageCreatedDto, ErrorCodes.ErrorDetail>> SaveAsync(
            ImageCreateDto dto)
        {
            if (dto.File == null || string.IsNullOrWhiteSpace(dto.File.FileName))
            {
                return ErrorCodes.NoImageFileProvided;
            }

            if (string.IsNullOrWhiteSpace(dto.Category.ToEnumString()))
            {
                return ErrorCodes.NoImageCategoryProvided;
            }

            // GUID logique commun pour toutes les variantes (webp/jpg)
            string imageId = Guid.NewGuid().ToString("N");
            string categorySlug = dto.Category.ToEnumMinusString();

            await using Stream sourceStream = dto.File.OpenReadStream();

            try
            {
                // 1. Métadonnées GPS
                (double? latitude, double? longitude) =
                    await imageMetadataExtractorService.ExtractGeoCoordinatesAsync(sourceStream);

                if (sourceStream.CanSeek)
                {
                    sourceStream.Position = 0;
                }

                // 2. Entité métier (à persister en Mongo dans un autre service)
                Image imageEntity = new()
                {
                    Id = imageId,
                    Category = dto.Category.MapTo<ImageCategoryDto, ImageCategory>(),
                    // On stocke la base de la clé de stockage, sans extension
                    Path = $"{categorySlug}/{imageId}",
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Latitude = latitude ?? 0,
                    Longitude = longitude ?? 0
                };

                // 3. Choix du flux à utiliser pour la compression
                Stream processingStream = sourceStream;
                Stream? watermarkedStream = null;

                if (dto.WithWatermark)
                {
                    watermarkedStream = await waterMarkService.ApplyWatermarkAsync(sourceStream, WatermarkText);
                    processingStream = watermarkedStream;

                    if (processingStream.CanSeek)
                    {
                        processingStream.Position = 0;
                    }
                }

                // 4. Compression + stockage Minio
                IEnumerable<string> savedFiles =
                    await ProcessAndStoreAsync(processingStream, imageId, categorySlug);

                if (watermarkedStream is not null)
                {
                    await watermarkedStream.DisposeAsync();
                }

                // 5. Résultat renvoyé à l'appelant
                return new ImageCreatedDto
                {
                    Id = imageEntity.Id,
                    SavedListFile = savedFiles,
                    Category = imageEntity.Category.MapTo<ImageCategory, ImageCategoryDto>(),
                    Latitude = imageEntity.Latitude,
                    Longitude = imageEntity.Longitude
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur pendant le traitement de l'image.");
                return ErrorCodes.ImageServorInternalError;
            }
        }

        /// <summary>
        /// Compresse et stocke l'image depuis le flux fourni.
        /// Le flux n'est PAS disposé ici : c'est la responsabilité de l'appelant.
        /// </summary>
        private async Task<IEnumerable<string>> ProcessAndStoreAsync(
            Stream imageStream,
            string baseName,
            string category)
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            Dictionary<string, byte[]> images =
                await imageCompressorService.CompressAsync(imageStream, baseName);

            return await imageStorageService.StoreAsync(images, category);
        }
    }
}
