using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dtos.Images;
using Dtos.Images.Creating;
using Dtos.Images.Links;
using Entities.Model.Images;
using Entities.Model.Users;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces.Images;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations.Images
{
    public class ImageLinksService : IImageLinksService
    {
        private readonly IImagesQueryHandler imagesQueryHandler;
        private readonly IUserQueryHandler userQueryHandler;
        private readonly IParksQueryHandler parksQueryHandler;

        public ImageLinksService(
            IImagesQueryHandler imagesQueryHandler,
            IUserQueryHandler userQueryHandler,
            IParksQueryHandler parksQueryHandler)
        {
            this.imagesQueryHandler = imagesQueryHandler;
            this.userQueryHandler = userQueryHandler;
            this.parksQueryHandler = parksQueryHandler;
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

            await SynchronizeOwnerAfterCurrentImageChangeAsync(updated);

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

            await SynchronizeOwnerAfterCurrentImageChangeAsync(updated);

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

            await SynchronizeOwnerAfterImageDeletionAsync(image);

            return true;
        }

        // -----------------------------------------------------------------------
        // Synchronisation de l'owner après changement du logo courant
        // -----------------------------------------------------------------------

        private async Task SynchronizeOwnerAfterCurrentImageChangeAsync(Image image)
        {
            if (IsUserAvatar(image))
            {
                await SynchronizeUserAvatarAsync(image);
                return;
            }

            if (IsParkLogo(image))
            {
                await SynchronizeParkCurrentLogoAsync(image.OwnerId!, image.IsCurrent ? image.Id : null);
            }
        }

        private async Task SynchronizeOwnerAfterImageDeletionAsync(Image image)
        {
            if (IsUserAvatar(image))
            {
                await SynchronizeUserAvatarAfterDeletionAsync(image);
                return;
            }

            if (IsParkLogo(image))
            {
                await SynchronizeParkLogoAfterDeletionAsync(image);
            }
        }

        // -----------------------------------------------------------------------
        // Logique de synchronisation — avatars utilisateur (inchangée)
        // -----------------------------------------------------------------------

        private async Task SynchronizeUserAvatarAsync(Image image)
        {
            User? user = await userQueryHandler.GetUserByIdAsync(image.OwnerId!);
            if (user == null)
            {
                return;
            }

            user.AvatarUrl = image.IsCurrent ? BuildAvatarUrl(image.Id) : null;
            user.UpdatedAt = DateTime.UtcNow;
            await userQueryHandler.UpdateUserAsync(user);
        }

        private async Task SynchronizeUserAvatarAfterDeletionAsync(Image image)
        {
            User? user = await userQueryHandler.GetUserByIdAsync(image.OwnerId!);
            if (user == null)
            {
                return;
            }

            IReadOnlyList<Image> remainingImages = await imagesQueryHandler.GetImagesByOwnerAsync(
                ImageOwnerType.User,
                image.OwnerId!,
                ImageCategory.AVATAR);

            Image? replacementCurrent = remainingImages.FirstOrDefault(img => img.IsCurrent);

            if (replacementCurrent == null)
            {
                replacementCurrent = remainingImages.FirstOrDefault();
                if (replacementCurrent != null)
                {
                    await imagesQueryHandler.UnsetCurrentImagesAsync(
                        replacementCurrent.OwnerType,
                        replacementCurrent.OwnerId!,
                        replacementCurrent.Category,
                        replacementCurrent.Id);

                    replacementCurrent.IsCurrent = true;
                    replacementCurrent.UpdatedAt = DateTime.UtcNow;
                    replacementCurrent = await imagesQueryHandler.UpdateImageAsync(replacementCurrent);
                }
            }

            user.AvatarUrl = replacementCurrent != null ? BuildAvatarUrl(replacementCurrent.Id) : null;
            user.UpdatedAt = DateTime.UtcNow;
            await userQueryHandler.UpdateUserAsync(user);
        }

        // -----------------------------------------------------------------------
        // Logique de synchronisation — logos de parcs
        // -----------------------------------------------------------------------

        /// <summary>
        /// Met à jour Park.CurrentLogoImageId après qu'un logo a été défini comme courant
        /// ou retiré du statut courant.
        /// </summary>
        private async Task SynchronizeParkCurrentLogoAsync(string parkId, string? currentLogoImageId)
        {
            await parksQueryHandler.UpdateCurrentLogoAsync(parkId, currentLogoImageId);
        }

        /// <summary>
        /// Recalcule Park.CurrentLogoImageId après la suppression d'un logo.
        /// Relit les logos restants pour trouver l'éventuel logo courant.
        /// </summary>
        private async Task SynchronizeParkLogoAfterDeletionAsync(Image deletedImage)
        {
            if (string.IsNullOrWhiteSpace(deletedImage.OwnerId))
            {
                return;
            }

            IReadOnlyList<Image> remainingLogos = await imagesQueryHandler.GetImagesByOwnerAsync(
                ImageOwnerType.Park,
                deletedImage.OwnerId,
                ImageCategory.PARK_LOGO);

            Image? currentLogo = remainingLogos.FirstOrDefault(img => img.IsCurrent);

            await parksQueryHandler.UpdateCurrentLogoAsync(deletedImage.OwnerId, currentLogo?.Id);
        }

        // -----------------------------------------------------------------------
        // Helpers statiques
        // -----------------------------------------------------------------------

        private static bool IsUserAvatar(Image image)
        {
            return image.OwnerType == ImageOwnerType.User
                   && image.Category == ImageCategory.AVATAR
                   && !string.IsNullOrWhiteSpace(image.OwnerId);
        }

        private static bool IsParkLogo(Image image)
        {
            return image.OwnerType == ImageOwnerType.Park
                   && image.Category == ImageCategory.PARK_LOGO
                   && !string.IsNullOrWhiteSpace(image.OwnerId);
        }

        private static string BuildAvatarUrl(string imageId)
        {
            return $"/images/{imageId}";
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
