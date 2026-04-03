using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Extensions;
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

                (double? latitude, double? longitude) =
                    await imageMetadataExtractorService.ExtractGeoCoordinatesAsync(request.FileStream);

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
                    Latitude = latitude ?? 0,
                    Longitude = longitude ?? 0,
                    OriginalFileName = string.IsNullOrWhiteSpace(request.OriginalFileName) ? null : request.OriginalFileName,
                    ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? null : request.ContentType
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

#if DEBUG
                    string debugDir = Path.Combine(Directory.GetCurrentDirectory(), "debug-images");
                    Directory.CreateDirectory(debugDir);
                    string debugPath = Path.Combine(debugDir, $"{imageId}_watermarked.jpg");

                    await using (FileStream debugStream = File.Create(debugPath))
                    {
                        if (processingStream.CanSeek)
                        {
                            processingStream.Position = 0;
                        }

                        await processingStream.CopyToAsync(debugStream);

                        if (processingStream.CanSeek)
                        {
                            processingStream.Position = 0;
                        }
                    }
#endif
                }

                IEnumerable<string> savedFiles = await ProcessAndStoreAsync(processingStream, imageId, categorySlug);

                if (watermarkedStream is not null)
                {
                    await watermarkedStream.DisposeAsync();
                }

                Image? savedImage = await imagesQueryHandler.CreateImageAsync(imageEntity);
                if (savedImage is null)
                {
                    logger.LogError(
                        "Échec de la persistance Mongo pour l'image {ImageId} (category={Category}, path={Path})",
                        imageEntity.Id,
                        imageEntity.Category,
                        imageEntity.Path);

                    return ErrorCodes.ImageServorInternalError;
                }

                return new ImageCreatedDto
                {
                    Id = savedImage.Id,
                    SavedListFile = savedFiles,
                    Category = savedImage.Category.MapTo<ImageCategory, ImageCategoryDto>(),
                    Latitude = savedImage.Latitude,
                    Longitude = savedImage.Longitude
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur pendant le traitement de l'image.");
                return ErrorCodes.ImageServorInternalError;
            }
        }

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
