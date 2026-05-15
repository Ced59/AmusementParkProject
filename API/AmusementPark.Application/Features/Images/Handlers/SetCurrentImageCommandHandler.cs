using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de promotion d'une image en image courante.
/// </summary>
public sealed class SetCurrentImageCommandHandler : ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>
{
    private readonly IImageRepository imageRepository;
    private readonly IParkRepository parkRepository;
    private readonly IUserRepository userRepository;

    public SetCurrentImageCommandHandler(IImageRepository imageRepository, IParkRepository parkRepository, IUserRepository userRepository)
    {
        this.imageRepository = imageRepository;
        this.parkRepository = parkRepository;
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<Image>> HandleAsync(SetCurrentImageCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ImageId))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
        }

        try
        {
            Image? image = await this.imageRepository.GetByIdAsync(command.ImageId.Trim(), cancellationToken);
            if (image is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
            }

            if (image.OwnerType == ImageOwnerType.None || string.IsNullOrWhiteSpace(image.OwnerId))
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotLinkedToOwner());
            }

            Image? updated = await this.imageRepository.SetCurrentAsync(image.Id, image.OwnerType, image.OwnerId, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorSettingCurrentImage());
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
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorSettingCurrentImage());
        }
    }

    private static async Task SynchronizeOwnerAsync(Image image, IParkRepository parkRepository, IUserRepository userRepository, CancellationToken cancellationToken)
    {
        if (image.OwnerType == ImageOwnerType.User && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            User? user = await userRepository.GetByIdAsync(image.OwnerId, cancellationToken);
            if (user is not null)
            {
                user.AvatarUrl = BuildImageUrl(image.Id);
                await userRepository.UpdateAsync(user.Id, user, cancellationToken);
            }

            return;
        }

        if (image.OwnerType == ImageOwnerType.Park && image.Category == ImageCategory.ParkLogo && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            Park? park = await parkRepository.GetByIdAsync(image.OwnerId, true, cancellationToken);
            if (park is not null)
            {
                park.CurrentLogoImageId = image.Id;
                await parkRepository.UpdateAsync(park.Id, park, cancellationToken);
            }
        }
    }

    private static string BuildImageUrl(string imageId)
    {
        return $"/images/{imageId}";
    }
}
