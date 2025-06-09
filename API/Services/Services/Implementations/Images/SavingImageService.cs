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

            if (string.IsNullOrWhiteSpace(dto.Category.ToEnumString()))
                return ErrorCodes.NoImageCategoryProvided;

            string baseName = Path.GetFileNameWithoutExtension(dto.File.FileName);
            Stream sourceStream = dto.File.OpenReadStream();

            (double? latitude, double? longitude) = await imageMetadataExtractorService.ExtractGeoCoordinatesAsync(sourceStream);

            // Préparation de l'entité métier
            Image imageEntity = new Image
            {
                Id = Guid.NewGuid().ToString(),
                Category = dto.Category.MapTo<ImageCategoryDto, ImageCategory>(),
                Path = dto.File.FileName,
                Description = dto.Description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Latitude = latitude ?? 0 ,
                Longitude = longitude ?? 0
            };

            try
            {
                if (dto.WithWatermark)
                {
                    
                    sourceStream = await waterMarkService.ApplyWatermarkAsync(sourceStream, WatermarkText);
                    sourceStream.Position = 0;

                    await using (FileStream debugStream = File.Create("test_watermarked.png"))
                    {
                        sourceStream.Position = 0;
                        await sourceStream.CopyToAsync(debugStream);
                    }

                }

                IEnumerable<string> savedFiles = await ProcessAndStoreAsync(sourceStream, baseName, dto.Category.ToEnumMinusString());

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
        /// </summary>
        private async Task<IEnumerable<string>> ProcessAndStoreAsync(
            Stream imageStream,
            string baseName,
            string category)
        {
            await using (imageStream)
            {
                Dictionary<string, byte[]> images = await imageCompressorService.CompressAsync(imageStream, baseName);
                return await imageStorageService.StoreAsync(images, category);
            }
        }
    }
}