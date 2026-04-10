using Common.Extensions;
using Common.General.Localization;
using Dtos.Images.Creating;
using Entities.Model.Errors;
using Entities.Model.Images;
using Microsoft.Extensions.Logging;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces.Images;
using Services.Models.Images;

namespace Services.Implementations.Images
{
    public class SavingImageService : ISavingImageService
    {
        private const string WatermarkText = "amusement-park.fun";

        private readonly IImageCompressorService imageCompressorService;
        private readonly IImageStorageService imageStorageService;
        private readonly IWaterMarkService waterMarkService;
        private readonly IImageMetadataExtractorService imageMetadataExtractorService;
        private readonly IImagesQueryHandler imagesQueryHandler;
        private readonly ILogger<SavingImageService> logger;

        public SavingImageService(
            IImageCompressorService imageCompressorService,
            IImageStorageService imageStorageService,
            IWaterMarkService waterMarkService,
            IImageMetadataExtractorService imageMetadataExtractorService,
            IImagesQueryHandler imagesQueryHandler,
            ILogger<SavingImageService> logger)
        {
            this.imageCompressorService = imageCompressorService;
            this.imageStorageService = imageStorageService;
            this.waterMarkService = waterMarkService;
            this.imageMetadataExtractorService = imageMetadataExtractorService;
            this.imagesQueryHandler = imagesQueryHandler;
            this.logger = logger;
        }

        public async Task<OneOf<ImageCreatedDto, ErrorCodes.ErrorDetail>> SaveAsync(ImageCreateDto dto)
        {
            if (dto.File == null || string.IsNullOrWhiteSpace(dto.File.FileName))
            {
                return ErrorCodes.NoImageFileProvided;
            }

            await using Stream fileStream = dto.File.OpenReadStream();

            ImageSaveRequest request = new()
            {
                FileStream = fileStream,
                Category = dto.Category,
                OriginalFileName = dto.File.FileName,
                ContentType = dto.File.ContentType,
                Description = dto.Description,
                WithWatermark = dto.WithWatermark
            };

            return await SaveAsync(request);
        }

        public async Task<OneOf<ImageCreatedDto, ErrorCodes.ErrorDetail>> SaveAsync(ImageSaveRequest request)
        {
            if (request.FileStream == null)
            {
                return ErrorCodes.NoImageFileProvided;
            }

            if (string.IsNullOrWhiteSpace(request.Category.ToEnumString()))
            {
                return ErrorCodes.NoImageCategoryProvided;
            }

            string imageId = Guid.NewGuid().ToString("N");
            string categorySlug = request.Category.ToEnumMinusString();

            try
            {
                if (request.FileStream.CanSeek)
                {
                    request.FileStream.Position = 0;
                }

                ExtractedImageMetadata metadata = await imageMetadataExtractorService.ExtractMetadataAsync(request.FileStream);

                if (request.FileStream.CanSeek)
                {
                    request.FileStream.Position = 0;
                }

                Image imageEntity = new()
                {
                    Id = imageId,
                    Category = request.Category.MapTo<ImageCategoryDto, ImageCategory>(),
                    Path = $"{categorySlug}/{imageId}",
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OriginalFileName = string.IsNullOrWhiteSpace(request.OriginalFileName) ? null : request.OriginalFileName,
                    ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? null : request.ContentType,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    SizeInBytes = metadata.SizeInBytes,
                    ExifMetadata = metadata.ExifMetadata,
                    GeoLocation = metadata.Latitude.HasValue && metadata.Longitude.HasValue
                        ? new ImageGeoLocation { Latitude = metadata.Latitude.Value, Longitude = metadata.Longitude.Value }
                        : null,
                    Captions = string.IsNullOrWhiteSpace(request.Description)
                        ? new List<LocalizedItem<string>>()
                        : new List<LocalizedItem<string>> { new() { LanguageCode = "fr", Value = request.Description } }
                };

                Stream processingStream = request.FileStream;
                Stream? watermarkedStream = null;

                if (request.WithWatermark)
                {
                    watermarkedStream = await waterMarkService.ApplyWatermarkAsync(request.FileStream, WatermarkText);
                    processingStream = watermarkedStream;

                    if (processingStream.CanSeek)
                    {
                        processingStream.Position = 0;
                    }
                }

                IEnumerable<string> savedFiles = await ProcessAndStoreAsync(processingStream, imageId, categorySlug);

                if (watermarkedStream is not null)
                {
                    await watermarkedStream.DisposeAsync();
                }

                Image? savedImage = await imagesQueryHandler.CreateImageAsync(imageEntity);
                if (savedImage is null)
                {
                    logger.LogError("Échec de la persistance Mongo pour l'image {ImageId} (category={Category}, path={Path})", imageEntity.Id, imageEntity.Category, imageEntity.Path);
                    return ErrorCodes.ImageServorInternalError;
                }

                return new ImageCreatedDto
                {
                    Id = savedImage.Id,
                    SavedListFile = savedFiles,
                    Category = savedImage.Category.MapTo<ImageCategory, ImageCategoryDto>(),
                    Latitude = savedImage.GeoLocation?.Latitude,
                    Longitude = savedImage.GeoLocation?.Longitude,
                    Width = savedImage.Width,
                    Height = savedImage.Height,
                    SizeInBytes = savedImage.SizeInBytes
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur pendant le traitement de l'image.");
                return ErrorCodes.ImageServorInternalError;
            }
        }

        private async Task<IEnumerable<string>> ProcessAndStoreAsync(Stream imageStream, string baseName, string category)
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            Dictionary<string, byte[]> images = await imageCompressorService.CompressAsync(imageStream, baseName);
            return await imageStorageService.StoreAsync(images, category);
        }
    }
}
