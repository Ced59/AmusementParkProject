using AmusementPark.Application.Abstractions;
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

public sealed class ImportRemoteImageCommandHandler : ICommandHandler<ImportRemoteImageCommand, ApplicationResult<Image>>
{
    private readonly IRemoteImageImporter remoteImageImporter;
    private readonly IImageRepository imageRepository;
    private readonly IParkRepository parkRepository;
    private readonly IUserRepository userRepository;

    public ImportRemoteImageCommandHandler(
        IRemoteImageImporter remoteImageImporter,
        IImageRepository imageRepository,
        IParkRepository parkRepository,
        IUserRepository userRepository)
    {
        this.remoteImageImporter = remoteImageImporter;
        this.imageRepository = imageRepository;
        this.parkRepository = parkRepository;
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<Image>> HandleAsync(ImportRemoteImageCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult<Image>.Failure(ApplicationErrors.Required(nameof(command.Request)));
        }

        string? sourceUrl = Normalize(command.Request.SourceUrl);
        if (sourceUrl is null || !Uri.TryCreate(sourceUrl, UriKind.Absolute, out Uri? sourceUri) || !IsHttpUri(sourceUri))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.RemoteImageSourceInvalid());
        }

        string? ownerId = Normalize(command.Request.OwnerId);
        if (command.Request.OwnerType != ImageOwnerType.None && ownerId is null)
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.InvalidOwner());
        }

        if (command.Request.SetAsCurrent && (command.Request.OwnerType == ImageOwnerType.None || ownerId is null))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.InvalidOwner());
        }

        try
        {
            RemoteImageImportRequest importRequest = new RemoteImageImportRequest
            {
                SourceUrl = sourceUrl,
                Category = command.Request.Category,
                OwnerType = command.Request.OwnerType,
                OwnerId = ownerId,
                Description = Normalize(command.Request.Description),
                WithWatermark = ShouldApplyWatermark(command.Request.Category, command.Request.WithWatermark),
                SetAsCurrent = command.Request.SetAsCurrent,
            };

            Image? image = await this.remoteImageImporter.ImportAsync(importRequest, cancellationToken);
            if (image is null)
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.RemoteImageImportFailed());
            }

            if (importRequest.SetAsCurrent && importRequest.OwnerId is not null)
            {
                Image? current = await this.imageRepository.SetCurrentAsync(image.Id, importRequest.OwnerType, importRequest.OwnerId, cancellationToken);
                if (current is null)
                {
                    return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorSettingCurrentImage());
                }

                image = current;
                await SynchronizeOwnerAsync(image, this.parkRepository, this.userRepository, cancellationToken);
            }

            return ApplicationResult<Image>.Success(image);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.RemoteImageImportFailed());
        }
    }

    private static bool IsHttpUri(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    private static bool ShouldApplyWatermark(ImageCategory category, bool requestedWithWatermark)
    {
        return requestedWithWatermark && category != ImageCategory.ParkLogo;
    }

    private static string? Normalize(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
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
