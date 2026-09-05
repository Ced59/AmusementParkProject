using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de suppression d'image.
/// </summary>
public sealed class DeleteImageCommandHandler : ICommandHandler<DeleteImageCommand, ApplicationResult>
{
    private readonly IImageRepository imageRepository;
    private readonly IImageBinaryStorage imageBinaryStorage;
    private readonly IParkRepository parkRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IUserRepository userRepository;
    private readonly ICommentRepository commentRepository;
    private readonly IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard;
    private readonly IPublicSeoUpdateNotifier? publicSeoUpdateNotifier;

    public DeleteImageCommandHandler(
        IImageRepository imageRepository,
        IImageBinaryStorage imageBinaryStorage,
        IParkRepository parkRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IUserRepository userRepository,
        ICommentRepository commentRepository,
        IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard,
        IPublicSeoUpdateNotifier? publicSeoUpdateNotifier = null)
    {
        this.imageRepository = imageRepository;
        this.imageBinaryStorage = imageBinaryStorage;
        this.parkRepository = parkRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.userRepository = userRepository;
        this.commentRepository = commentRepository;
        this.shareSourceRevisionGuard = shareSourceRevisionGuard;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public async Task<ApplicationResult> HandleAsync(DeleteImageCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ImageId))
        {
            return ApplicationResult.Failure(ImageApplicationErrors.ImageNotExists());
        }

        try
        {
            Image? image = await this.imageRepository.GetByIdAsync(command.ImageId.Trim(), cancellationToken);
            if (image is null)
            {
                return ApplicationResult.Failure(ImageApplicationErrors.ImageNotExists());
            }

            if (ManagedCommentImageMutationGuard.IsManagedScope(image))
            {
                return ApplicationResult.Failure(
                    ImageApplicationErrors.CommentImageLifecycleManaged());
            }

            if (await this.commentRepository.IsImageReferencedAsync(image.Id, cancellationToken))
            {
                return ApplicationResult.Failure(ImageApplicationErrors.ImageReferencedByComment());
            }
            IReadOnlyCollection<string> avatarOwnerUserIds =
                UserAvatarShareSourceMutation.ResolveImpactedOwnerUserIds(
                    image,
                    ImageOwnerType.None,
                    null,
                    image.Category);
            IReadOnlyDictionary<string, ShareSourceMutationLease> avatarMutationLeases =
                await UserAvatarShareSourceMutation.BeginAsync(
                    avatarOwnerUserIds,
                    this.shareSourceRevisionGuard,
                    cancellationToken);
            bool avatarSourceChanged = false;
            try
            {
                avatarSourceChanged = avatarOwnerUserIds.Count > 0;
                if (!string.IsNullOrWhiteSpace(image.Path))
                {
                    bool binaryDeleted = await this.imageBinaryStorage.DeleteAsync(
                        image.Path,
                        cancellationToken);
                    if (!binaryDeleted)
                    {
                        return ApplicationResult.Failure(ImageApplicationErrors.ErrorDeletingImage());
                    }
                }

                bool deleted = await this.imageRepository.DeleteAsync(image.Id, cancellationToken);
                if (!deleted)
                {
                    return ApplicationResult.Failure(ImageApplicationErrors.ErrorDeletingImage());
                }

                await SynchronizeAfterDeletionAsync(
                    image,
                    this.imageRepository,
                    this.parkRepository,
                    this.attractionManufacturerRepository,
                    this.searchProjectionWriter,
                    cancellationToken);
                await UserAvatarShareSourceMutation.SynchronizeAsync(
                    avatarOwnerUserIds,
                    this.imageRepository,
                    this.userRepository,
                    cancellationToken);
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
                new[] { image },
                Array.Empty<Image>(),
                cancellationToken);
            return ApplicationResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult.Failure(ImageApplicationErrors.ErrorDeletingImage());
        }
    }

    private static async Task SynchronizeAfterDeletionAsync(
        Image image,
        IImageRepository imageRepository,
        IParkRepository parkRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ISearchProjectionWriter searchProjectionWriter,
        CancellationToken cancellationToken)
    {
        if (image.OwnerType == ImageOwnerType.User && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            IReadOnlyCollection<Image> remainingImages = await imageRepository.GetByOwnerAsync(ImageOwnerType.User, image.OwnerId, ImageCategory.Avatar, cancellationToken);
            Image? replacementCurrent = remainingImages.FirstOrDefault(static candidate => candidate.IsCurrent);

            if (replacementCurrent is null)
            {
                Image? firstRemaining = remainingImages.FirstOrDefault();
                if (firstRemaining is not null)
                {
                    replacementCurrent = await imageRepository.SetCurrentAsync(firstRemaining.Id, ImageOwnerType.User, image.OwnerId, cancellationToken);
                }
            }
            return;
        }

        if (image.OwnerType == ImageOwnerType.Park && image.Category == ImageCategory.Logo && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            Park? park = await parkRepository.GetByIdAsync(image.OwnerId, true, cancellationToken);
            if (park is null)
            {
                return;
            }

            Image? currentLogo = await imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.Park, image.OwnerId, ImageCategory.Logo, cancellationToken);
            park.CurrentLogoImageId = currentLogo?.Id;
            await parkRepository.UpdateAsync(park.Id, park, cancellationToken);
            return;
        }

        if (image.OwnerType == ImageOwnerType.AttractionManufacturer && image.Category == ImageCategory.Logo && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            AttractionManufacturer? manufacturer = await attractionManufacturerRepository.GetByIdAsync(image.OwnerId, cancellationToken);
            if (manufacturer is null)
            {
                return;
            }

            Image? currentLogo = await imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.AttractionManufacturer, image.OwnerId, ImageCategory.Logo, cancellationToken);
            manufacturer.CurrentLogoImageId = currentLogo?.Id;
            await attractionManufacturerRepository.UpdateAsync(manufacturer.Id, manufacturer, cancellationToken);
            await searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, manufacturer.Id, cancellationToken);
        }
    }

}
