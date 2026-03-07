using Dtos.Images;
using Dtos.Images.Creating;
using Dtos.Images.Links;
using Entities.Model.Images;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces.Images;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations.Images
{
    public class ImageLinksService : IImageLinksService
    {
        private readonly IImagesQueryHandler imagesQueryHandler;

        public ImageLinksService(IImagesQueryHandler imagesQueryHandler)
        {
            this.imagesQueryHandler = imagesQueryHandler;
        }

        public async Task<OneOf<ImageDto, ErrorDetail>> LinkImageAsync(LinkImageToOwnerDto request)
        {
            Image? image = await imagesQueryHandler.GetImageByIdAsync(request.ImageId);

            if (image == null)
            {
                return ImageNotExists;
            }

            ImageOwnerType ownerType = MapOwnerType(request.OwnerType);

            image.OwnerType = ownerType;
            image.OwnerId = request.OwnerId;
            image.Description = request.Description ?? image.Description;
            image.IsCurrent = request.SetAsCurrent;
            image.UpdatedAt = DateTime.UtcNow;

            if (request.SetAsCurrent)
            {
                await imagesQueryHandler.UnsetCurrentImagesAsync(
                    ownerType,
                    request.OwnerId,
                    image.Category,
                    image.Id);
            }

            Image? updated = await imagesQueryHandler.UpdateImageAsync(image);

            if (updated == null)
            {
                return ErrorUpdatingImageLink;
            }

            return Map(updated);
        }

        public async Task<OneOf<ImageDto, ErrorDetail>> GetCurrentImageAsync(
            string ownerId,
            ImageOwnerType ownerType,
            ImageCategory category)
        {
            Image? image = await imagesQueryHandler.GetCurrentImageByOwnerAsync(ownerType, ownerId, category);

            if (image == null)
            {
                return ImageNotExists;
            }

            return Map(image);
        }

        public async Task<OneOf<IEnumerable<ImageDto>, ErrorDetail>> GetImagesAsync(
            string ownerId,
            ImageOwnerType ownerType,
            ImageCategory category)
        {
            IReadOnlyList<Image> images = await imagesQueryHandler.GetImagesByOwnerAsync(ownerType, ownerId, category);

            List<ImageDto> mappedImages = images
                .Select(Map)
                .ToList();

            OneOf<IEnumerable<ImageDto>, ErrorDetail> result = mappedImages;
            return result;
        }

        public async Task<OneOf<ImageDto, ErrorDetail>> SetCurrentImageAsync(string imageId)
        {
            Image? image = await imagesQueryHandler.GetImageByIdAsync(imageId);

            if (image == null)
            {
                return ImageNotExists;
            }

            if (image.OwnerType == ImageOwnerType.None || string.IsNullOrWhiteSpace(image.OwnerId))
            {
                return ImageNotLinkedToOwner;
            }

            await imagesQueryHandler.UnsetCurrentImagesAsync(
                image.OwnerType,
                image.OwnerId,
                image.Category,
                image.Id);

            image.IsCurrent = true;
            image.UpdatedAt = DateTime.UtcNow;

            Image? updated = await imagesQueryHandler.UpdateImageAsync(image);

            if (updated == null)
            {
                return ErrorSettingCurrentImage;
            }

            return Map(updated);
        }

        public async Task<OneOf<bool, ErrorDetail>> DeleteImageAsync(string imageId)
        {
            Image? image = await imagesQueryHandler.GetImageByIdAsync(imageId);

            if (image == null)
            {
                return ImageNotExists;
            }

            bool deleted = await imagesQueryHandler.DeleteImageAsync(imageId);

            if (!deleted)
            {
                return ErrorDeletingImage;
            }

            return true;
        }

        private static ImageDto Map(Image image)
        {
            return new ImageDto
            {
                Id = image.Id,
                Category = MapCategory(image.Category),
                OwnerType = MapOwnerType(image.OwnerType),
                OwnerId = image.OwnerId,
                Path = image.Path,
                Description = image.Description,
                IsCurrent = image.IsCurrent,
                CreatedAt = image.CreatedAt
            };
        }

        private static ImageOwnerType MapOwnerType(ImageOwnerTypeDto dto)
        {
            return dto switch
            {
                ImageOwnerTypeDto.PARK => ImageOwnerType.Park,
                ImageOwnerTypeDto.USER => ImageOwnerType.User,
                ImageOwnerTypeDto.ATTRACTION => ImageOwnerType.Attraction,
                _ => ImageOwnerType.None
            };
        }

        private static ImageOwnerTypeDto MapOwnerType(ImageOwnerType ownerType)
        {
            return ownerType switch
            {
                ImageOwnerType.Park => ImageOwnerTypeDto.PARK,
                ImageOwnerType.User => ImageOwnerTypeDto.USER,
                ImageOwnerType.Attraction => ImageOwnerTypeDto.ATTRACTION,
                _ => ImageOwnerTypeDto.NONE
            };
        }

        private static ImageCategoryDto MapCategory(ImageCategory category)
        {
            return category switch
            {
                ImageCategory.AVATAR => ImageCategoryDto.AVATAR,
                ImageCategory.PARK_LOGO => ImageCategoryDto.PARK_LOGO,
                ImageCategory.PARK => ImageCategoryDto.PARK,
                ImageCategory.ATTRACTION => ImageCategoryDto.ATTRACTION,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }
    }
}