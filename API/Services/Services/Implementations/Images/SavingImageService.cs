using System.ComponentModel;
using Common.Extensions;
using Services.Interfaces.Images;
using Dtos.Images.Creating;
using Entities.Model.Errors;
using Entities.Model.Images;
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

            Image imageToProcess = new()
            {
                Category = imageCreateDto.Category.MapTo<ImageCategoryDto, ImageCategory>(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Id = Guid.NewGuid().ToString(),
                Path = imageCreateDto.File.FileName,
                Description = imageCreateDto.Description,
                Longitude = 0,
                Latitude = 0
            };

            // TODO Créer le service d'extraction de métadonnées pour la latitude et longitude en envoyant l'entité métier

            // TODO Créer le respository pour l'enregistrement des images et enregistrer en envoyant l'entité métier. Faire les relations pour les MAJ dans les différentes collections.

            try
            {
                IEnumerable<string> savedListFile = await CompressAndStoreImage(imageCreateDto);

                return new ImageCreatedDto
                {
                    Id = imageToProcess.Id,
                    SavedListFile = savedListFile,
                    Category = imageToProcess.Category.MapTo<ImageCategory, ImageCategoryDto>(),
                    Latitude = imageToProcess.Latitude,
                    Longitude = imageToProcess.Longitude
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur pendant l'upload ou la compression d'images.");
                return ErrorCodes.ImageServorInternalError;
            }
        }

        private async Task<IEnumerable<string>> CompressAndStoreImage(ImageCreateDto imageCreateDto)
        {
            string baseName = Path.GetFileNameWithoutExtension(imageCreateDto.File!.FileName);

            await using Stream stream = imageCreateDto.File!.OpenReadStream();

            Dictionary<string, byte[]> images = await imageCompressorService.CompressAsync(stream, baseName);

            IEnumerable<string> savedListFile = await imageStorageService.StoreAsync(images, imageCreateDto.Category.ToEnumMinusString());
            return savedListFile;
        }
    }
}