using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
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
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IUserRepository userRepository;
    private readonly IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard;
    private readonly IPublicSeoUpdateNotifier? publicSeoUpdateNotifier;

    public ImportRemoteImageCommandHandler(
        IRemoteImageImporter remoteImageImporter,
        IImageRepository imageRepository,
        IParkRepository parkRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IUserRepository userRepository,
        IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard,
        IPublicSeoUpdateNotifier? publicSeoUpdateNotifier = null)
    {
        this.remoteImageImporter = remoteImageImporter;
        this.imageRepository = imageRepository;
        this.parkRepository = parkRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.userRepository = userRepository;
        this.shareSourceRevisionGuard = shareSourceRevisionGuard;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public async Task<ApplicationResult<Image>> HandleAsync(ImportRemoteImageCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult<Image>.Failure(ApplicationErrors.Required(nameof(command.Request)));
        }

        if (ManagedCommentImageMutationGuard.IsManagedScope(
            command.Request.Category,
            command.Request.OwnerType))
        {
            return ApplicationResult<Image>.Failure(
                ImageApplicationErrors.CommentImageLifecycleManaged());
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
            IReadOnlyCollection<string> avatarOwnerUserIds = importRequest.SetAsCurrent
                ? UserAvatarShareSourceMutation.ResolveImpactedOwnerUserIds(
                    null,
                    importRequest.OwnerType,
                    importRequest.OwnerId,
                    importRequest.Category)
                : Array.Empty<string>();
            IReadOnlyDictionary<string, ShareSourceMutationLease> avatarMutationLeases =
                await UserAvatarShareSourceMutation.BeginAsync(
                    avatarOwnerUserIds,
                    this.shareSourceRevisionGuard,
                    cancellationToken);
            bool avatarSourceChanged = false;
            Image? image;
            try
            {
                avatarSourceChanged = avatarOwnerUserIds.Count > 0;
                image = await this.remoteImageImporter.ImportAsync(importRequest, cancellationToken);
                if (image is null)
                {
                    return ApplicationResult<Image>.Failure(ImageApplicationErrors.RemoteImageImportFailed());
                }

                if (importRequest.SetAsCurrent && importRequest.OwnerId is not null)
                {
                    Image? current = await this.imageRepository.SetCurrentIfUnchangedAsync(
                        image.Id,
                        new ImageMutationPrecondition(
                            image.OwnerType,
                            image.OwnerId,
                            image.Category,
                            image.IsCurrent),
                        importRequest.OwnerType,
                        importRequest.OwnerId,
                        cancellationToken);
                    if (current is null)
                    {
                        return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorSettingCurrentImage());
                    }

                    image = current;
                    await UserAvatarShareSourceMutation.SynchronizeAsync(
                        avatarOwnerUserIds,
                        this.imageRepository,
                        this.userRepository,
                        cancellationToken);
                    await SynchronizeOwnerAsync(
                        image,
                        this.parkRepository,
                        this.attractionManufacturerRepository,
                        this.searchProjectionWriter,
                        cancellationToken);
                }
            }
            finally
            {
                await UserAvatarShareSourceMutation.CompleteAsync(
                    avatarMutationLeases,
                    avatarSourceChanged,
                    this.shareSourceRevisionGuard);
            }

            await PublicImageSeoUpdateNotification.NotifyAsync(
                this.publicSeoUpdateNotifier,
                Array.Empty<Image>(),
                new[] { image },
                cancellationToken);
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
        return requestedWithWatermark && !IsLogoCategory(category);
    }

    private static bool IsLogoCategory(ImageCategory category)
    {
        return category is ImageCategory.Logo;
    }

    private static string? Normalize(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static async Task SynchronizeOwnerAsync(
        Image image,
        IParkRepository parkRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ISearchProjectionWriter searchProjectionWriter,
        CancellationToken cancellationToken)
    {
        if (image.OwnerType == ImageOwnerType.Park && image.Category == ImageCategory.Logo && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            Park? park = await parkRepository.GetByIdAsync(image.OwnerId, true, cancellationToken);
            if (park is not null)
            {
                park.CurrentLogoImageId = image.Id;
                await parkRepository.UpdateAsync(park.Id, park, cancellationToken);
            }

            return;
        }

        if (image.OwnerType == ImageOwnerType.AttractionManufacturer && image.Category == ImageCategory.Logo && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            AttractionManufacturer? manufacturer = await attractionManufacturerRepository.GetByIdAsync(image.OwnerId, cancellationToken);
            if (manufacturer is not null)
            {
                manufacturer.CurrentLogoImageId = image.Id;
                await attractionManufacturerRepository.UpdateAsync(manufacturer.Id, manufacturer, cancellationToken);
                await searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, manufacturer.Id, cancellationToken);
            }
        }
    }

}
