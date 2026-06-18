using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de rattachement d'image à un propriétaire.
/// </summary>
public sealed class LinkImageCommandHandler : ICommandHandler<LinkImageCommand, ApplicationResult<Image>>
{
    private readonly IImageRepository imageRepository;
    private readonly IParkRepository parkRepository;
    private readonly IUserRepository userRepository;

    public LinkImageCommandHandler(IImageRepository imageRepository, IParkRepository parkRepository, IUserRepository userRepository)
    {
        this.imageRepository = imageRepository;
        this.parkRepository = parkRepository;
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<Image>> HandleAsync(LinkImageCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ImageId))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
        }

        if (command.OwnerType != ImageOwnerType.None && string.IsNullOrWhiteSpace(command.OwnerId))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.InvalidOwner());
        }

        try
        {
            Image? image = await this.imageRepository.GetByIdAsync(command.ImageId.Trim(), cancellationToken);
            if (image is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
            }

            string? normalizedOwnerId = string.IsNullOrWhiteSpace(command.OwnerId) ? null : command.OwnerId.Trim();
            Image? updated = command.SetAsCurrent
                ? await this.imageRepository.SetCurrentAsync(image.Id, command.OwnerType, normalizedOwnerId ?? string.Empty, cancellationToken)
                : await this.imageRepository.LinkAsync(image.Id, command.OwnerType, normalizedOwnerId ?? string.Empty, cancellationToken);

            if (updated is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorUpdatingImageLink());
            }

            if (command.Description is not null)
            {
                ImageMetadataUpdate metadata = new ImageMetadataUpdate
                {
                    Description = command.Description,
                    GeoLocation = updated.GeoLocation == null ? null : new GeoPointValue(updated.GeoLocation.Latitude, updated.GeoLocation.Longitude),
                    AltTexts = updated.AltTexts.Select(static value => new LocalizedTextValue(value.LanguageCode, value.Value)).ToList(),
                    Captions = updated.Captions.Select(static value => new LocalizedTextValue(value.LanguageCode, value.Value)).ToList(),
                    Credits = updated.Credits.Select(static value => new LocalizedTextValue(value.LanguageCode, value.Value)).ToList(),
                    TagIds = updated.TagIds.ToList(),
                    Category = updated.Category,
                    IsPublished = updated.IsPublished,
                    SourceUrl = updated.SourceUrl,
                };

                Image? metadataUpdated = await this.imageRepository.UpdateMetadataAsync(updated.Id, metadata, cancellationToken);
                if (metadataUpdated is not null)
                {
                    updated = metadataUpdated;
                }
            }

            await SynchronizeOwnerAsync(updated, this.parkRepository, this.userRepository, cancellationToken);
            return ApplicationResult<Image>.Success(updated);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorUpdatingImageLink());
        }
    }

    private static async Task SynchronizeOwnerAsync(Image image, IParkRepository parkRepository, IUserRepository userRepository, CancellationToken cancellationToken)
    {
        if (image.OwnerType == ImageOwnerType.User && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            User? user = await userRepository.GetByIdAsync(image.OwnerId, cancellationToken);
            if (user is not null)
            {
                user.AvatarUrl = image.IsCurrent ? BuildImageUrl(image.Id) : null;
                await userRepository.UpdateAsync(user.Id, user, cancellationToken);
            }

            return;
        }

        if (image.OwnerType == ImageOwnerType.Park && image.Category == ImageCategory.ParkLogo && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            Park? park = await parkRepository.GetByIdAsync(image.OwnerId, true, cancellationToken);
            if (park is not null)
            {
                park.CurrentLogoImageId = image.IsCurrent ? image.Id : null;
                await parkRepository.UpdateAsync(park.Id, park, cancellationToken);
            }
        }
    }

    private static string BuildImageUrl(string imageId)
    {
        return $"/images/{imageId}";
    }
}
