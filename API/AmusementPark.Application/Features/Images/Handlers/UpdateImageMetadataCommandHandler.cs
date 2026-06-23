using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de mise à jour des métadonnées d'image.
/// </summary>
public sealed class UpdateImageMetadataCommandHandler : ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>
{
    private readonly IImageRepository imageRepository;
    private readonly IParkRepository parkRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IUserRepository userRepository;

    public UpdateImageMetadataCommandHandler(
        IImageRepository imageRepository,
        IParkRepository parkRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IUserRepository userRepository)
    {
        this.imageRepository = imageRepository;
        this.parkRepository = parkRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<Image>> HandleAsync(UpdateImageMetadataCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ImageId))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
        }

        if (command.Metadata is null)
        {
            return ApplicationResult<Image>.Failure(ApplicationErrors.Required(nameof(command.Metadata)));
        }

        try
        {
            string normalizedImageId = command.ImageId.Trim();
            Image? existing = await this.imageRepository.GetByIdAsync(normalizedImageId, cancellationToken);
            if (existing is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
            }

            ImageMetadataUpdate metadata = BuildNormalizedMetadata(command.Metadata, existing);
            if (metadata.OwnerType != ImageOwnerType.None && string.IsNullOrWhiteSpace(metadata.OwnerId))
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.InvalidOwner());
            }

            bool scopeChanged = HasCurrentScopeChanged(existing, metadata);
            if (scopeChanged && existing.IsCurrent && metadata.IsCurrent != true)
            {
                metadata = CloneMetadata(metadata, false);
            }

            Image? updated = await this.imageRepository.UpdateMetadataAsync(normalizedImageId, metadata, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
            }

            if (scopeChanged && existing.IsCurrent)
            {
                await SynchronizeOwnerScopeAsync(existing.OwnerType, existing.OwnerId, existing.Category, this.imageRepository, this.parkRepository, this.attractionManufacturerRepository, this.searchProjectionWriter, this.userRepository, cancellationToken);
            }

            if (metadata.IsCurrent == true && updated.OwnerType != ImageOwnerType.None && !string.IsNullOrWhiteSpace(updated.OwnerId))
            {
                Image? current = await this.imageRepository.SetCurrentAsync(updated.Id, updated.OwnerType, updated.OwnerId, cancellationToken);
                if (current is null)
                {
                    return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorSettingCurrentImage());
                }

                updated = current;
            }

            if ((updated.IsCurrent || metadata.IsCurrent.HasValue || scopeChanged) && updated.OwnerType != ImageOwnerType.None)
            {
                await SynchronizeOwnerScopeAsync(updated.OwnerType, updated.OwnerId, updated.Category, this.imageRepository, this.parkRepository, this.attractionManufacturerRepository, this.searchProjectionWriter, this.userRepository, cancellationToken);
            }

            return ApplicationResult<Image>.Success(updated);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageProcessingFailed());
        }
    }

    private static ImageMetadataUpdate BuildNormalizedMetadata(ImageMetadataUpdate metadata, Image existing)
    {
        ImageOwnerType ownerType = metadata.OwnerType ?? existing.OwnerType;
        string? ownerId = ownerType == ImageOwnerType.None
            ? null
            : Normalize(metadata.OwnerType.HasValue ? metadata.OwnerId : existing.OwnerId);

        return new ImageMetadataUpdate
        {
            Description = metadata.Description,
            GeoLocation = metadata.GeoLocation,
            AltTexts = metadata.AltTexts,
            Captions = metadata.Captions,
            Credits = metadata.Credits,
            TagIds = metadata.TagIds,
            Category = metadata.Category,
            OwnerType = ownerType,
            OwnerId = ownerId,
            IsCurrent = metadata.IsCurrent,
            IsPublished = metadata.IsPublished,
            SourceUrl = metadata.SourceUrl,
        };
    }

    private static ImageMetadataUpdate CloneMetadata(ImageMetadataUpdate metadata, bool isCurrent)
    {
        return new ImageMetadataUpdate
        {
            Description = metadata.Description,
            GeoLocation = metadata.GeoLocation,
            AltTexts = metadata.AltTexts,
            Captions = metadata.Captions,
            Credits = metadata.Credits,
            TagIds = metadata.TagIds,
            Category = metadata.Category,
            OwnerType = metadata.OwnerType,
            OwnerId = metadata.OwnerId,
            IsCurrent = isCurrent,
            IsPublished = metadata.IsPublished,
            SourceUrl = metadata.SourceUrl,
        };
    }

    private static bool HasCurrentScopeChanged(Image existing, ImageMetadataUpdate metadata)
    {
        return existing.Category != metadata.Category ||
               existing.OwnerType != metadata.OwnerType ||
               !string.Equals(Normalize(existing.OwnerId), Normalize(metadata.OwnerId), StringComparison.Ordinal);
    }

    private static async Task SynchronizeOwnerScopeAsync(
        ImageOwnerType ownerType,
        string? ownerId,
        ImageCategory category,
        IImageRepository imageRepository,
        IParkRepository parkRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        if (ownerType == ImageOwnerType.None || string.IsNullOrWhiteSpace(ownerId))
        {
            return;
        }

        string normalizedOwnerId = ownerId.Trim();

        if (ownerType == ImageOwnerType.User && category == ImageCategory.Avatar)
        {
            User? user = await userRepository.GetByIdAsync(normalizedOwnerId, cancellationToken);
            if (user is null)
            {
                return;
            }

            Image? currentAvatar = await imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.User, normalizedOwnerId, ImageCategory.Avatar, cancellationToken);
            user.AvatarUrl = currentAvatar is null ? null : BuildImageUrl(currentAvatar.Id);
            await userRepository.UpdateAsync(user.Id, user, cancellationToken);
            return;
        }

        if (ownerType == ImageOwnerType.Park && category == ImageCategory.Logo)
        {
            Park? park = await parkRepository.GetByIdAsync(normalizedOwnerId, true, cancellationToken);
            if (park is null)
            {
                return;
            }

            Image? currentLogo = await imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.Park, normalizedOwnerId, ImageCategory.Logo, cancellationToken);
            park.CurrentLogoImageId = currentLogo?.Id;
            await parkRepository.UpdateAsync(park.Id, park, cancellationToken);
            return;
        }

        if (ownerType == ImageOwnerType.AttractionManufacturer && category == ImageCategory.Logo)
        {
            AttractionManufacturer? manufacturer = await attractionManufacturerRepository.GetByIdAsync(normalizedOwnerId, cancellationToken);
            if (manufacturer is null)
            {
                return;
            }

            Image? currentLogo = await imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.AttractionManufacturer, normalizedOwnerId, ImageCategory.Logo, cancellationToken);
            manufacturer.CurrentLogoImageId = currentLogo?.Id;
            await attractionManufacturerRepository.UpdateAsync(manufacturer.Id, manufacturer, cancellationToken);
            await searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, manufacturer.Id, cancellationToken);
        }
    }

    private static string? Normalize(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string BuildImageUrl(string imageId)
    {
        return $"/images/{imageId}";
    }
}
