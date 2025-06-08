using System.ComponentModel;
using Common.Extensions;
using Services.Interfaces.Images;
using Dtos.Images.Creating;
using Entities.Model.Errors;
using Microsoft.Extensions.Logging;
using OneOf;
using ZstdSharp;

namespace Services.Implementations.Images
{
    public class SavingImageService : ISavingImageService
    {
        private readonly IImageCompressorService imageCompressorService;

        private readonly IImageStorageService imageStorageService;

        private readonly ILogger<SavingImageService> logger;

        public SavingImageService(IImageCompressorService imageCompressorService, IImageStorageService imageStorageService, ILogger<SavingImageService> logger)
        {
            this.imageCompressorService = imageCompressorService;
            this.imageStorageService = imageStorageService;
            this.logger = logger;
        }

        public async Task<OneOf<ImageCreatedDto, ErrorCodes.ErrorDetail>> SaveAsync(ImageCreateDto imageCreateDto)
        {
            if (imageCreateDto.File == null || string.IsNullOrWhiteSpace(imageCreateDto.File.FileName))
                return ErrorCodes.NoImageFileProvided;

            if (string.IsNullOrWhiteSpace(imageCreateDto.Category.ToEnumString()))
                return ErrorCodes.NoImageCategoryProvided;

            try
            {
                string baseName = Path.GetFileNameWithoutExtension(imageCreateDto.File.FileName);

                await using Stream stream = imageCreateDto.File.OpenReadStream();

                Dictionary<string, byte[]> images = await imageCompressorService.CompressAsync(stream, baseName);

                IEnumerable<string> savedListFile = await imageStorageService.StoreAsync(images, imageCreateDto.Category.ToEnumMinusString());

                return new ImageCreatedDto
                {
                    Id = Guid.NewGuid().ToString(),
                    SavedListFile = savedListFile
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur pendant l'upload ou la compression d'images.");
                return ErrorCodes.ImageServorInternalError;
            }
        }
    }
}