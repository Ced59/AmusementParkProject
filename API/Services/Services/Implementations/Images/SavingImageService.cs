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
                return ErrorCodes.NoImageFileProvided;

            // Ça dépend de ton implémentation de ToEnumString, mais ça ne détecte pas forcément "aucune catégorie".
            if (string.IsNullOrWhiteSpace(dto.Category.ToEnumString()))
                return ErrorCodes.NoImageCategoryProvided;

            string baseName = Path.GetFileNameWithoutExtension(dto.File.FileName);

            await using Stream sourceStream = dto.File.OpenReadStream();

            try
            {
                // 1. Métadonnées EXIF
                (double? latitude, double? longitude) =
                    await imageMetadataExtractorService.ExtractGeoCoordinatesAsync(sourceStream);

                // On remet explicitement à zéro pour la suite du pipeline
                sourceStream.Position = 0;

                // 2. Préparation de l'entité métier
                Image imageEntity = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Category = dto.Category.MapTo<ImageCategoryDto, ImageCategory>(),
                    Path = dto.File.FileName,
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Latitude = latitude ?? 0,
                    Longitude = longitude ?? 0
                };

                // 3. Choix du flux à compresser : original ou filigrané
                Stream processingStream = sourceStream;
                Stream? watermarkedStream = null;

                if (dto.WithWatermark)
                {
                    watermarkedStream = await waterMarkService.ApplyWatermarkAsync(sourceStream, WatermarkText);
                    processingStream = watermarkedStream;
                    processingStream.Position = 0;

#if DEBUG
                    // Debug : on enregistre l'image filigranée pour inspection
                    await using (FileStream debugStream = File.Create("test_watermarked.jpg"))
                    {
                        processingStream.Position = 0;
                        await processingStream.CopyToAsync(debugStream);
                        processingStream.Position = 0;
                    }
#endif
                }

                // 4. Compression + stockage Minio
                IEnumerable<string> savedFiles =
                    await ProcessAndStoreAsync(processingStream, baseName, dto.Category.ToEnumMinusString());

                // On nettoie le flux filigrané éventuel
                if (watermarkedStream is not null)
                    await watermarkedStream.DisposeAsync();

                // 5. Résultat
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
        /// Le flux n'est PAS disposé ici (c'est le rôle de l'appelant).
        /// </summary>
        private async Task<IEnumerable<string>> ProcessAndStoreAsync(
            Stream imageStream,
            string baseName,
            string category)
        {
            // On s'assure juste d'être au début
            if (imageStream.CanSeek)
                imageStream.Position = 0;

            Dictionary<string, byte[]> images =
                await imageCompressorService.CompressAsync(imageStream, baseName);

            return await imageStorageService.StoreAsync(images, category);
        }
    }
}