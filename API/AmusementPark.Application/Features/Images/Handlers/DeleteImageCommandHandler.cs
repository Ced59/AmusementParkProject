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
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<DeleteImageCommandHandler>? logger;

    public DeleteImageCommandHandler(
        IImageRepository imageRepository,
        IImageBinaryStorage imageBinaryStorage,
        IParkRepository parkRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IUserRepository userRepository,
        ICommentRepository commentRepository,
        IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard,
        IPublicSeoUpdateNotifier? publicSeoUpdateNotifier = null,
        ILogger<DeleteImageCommandHandler>? logger = null)
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
        this.logger = logger;
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
            using CancellationTokenSource mutationCancellation =
                ShareSourceMutationCancellation.CreateLinkedSource(
                    cancellationToken,
                    avatarMutationLeases.Values);
            using CancellationTokenSource consistencyCancellation =
                ShareSourceMutationCancellation.CreateLeaseSource(
                    avatarMutationLeases.Values);
            bool avatarSourceChanged = false;
            try
            {
                avatarSourceChanged = avatarOwnerUserIds.Count > 0;
                bool deleted = await this.imageRepository.DeleteIfUnchangedAsync(
                    image.Id,
                    new ImageMutationPrecondition(
                        image.OwnerType,
                        image.OwnerId,
                        image.Category,
                        image.IsCurrent),
                    mutationCancellation.Token);
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
                    consistencyCancellation.Token);
                await UserAvatarShareSourceMutation.SynchronizeAsync(
                    avatarOwnerUserIds,
                    this.imageRepository,
                    this.userRepository,
                    consistencyCancellation.Token);
                if (!string.IsNullOrWhiteSpace(image.Path))
                {
                    await this.DeleteBinaryBestEffortAsync(image);
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

    private async Task DeleteBinaryBestEffortAsync(Image image)
    {
        try
        {
            bool deleted = await this.imageBinaryStorage.DeleteAsync(
                image.Path!,
                CancellationToken.None);
            if (!deleted)
            {
                this.logger?.LogWarning(
                    "Image metadata {ImageId} was deleted, but binary cleanup did not remove {ImagePath}.",
                    image.Id,
                    image.Path);
            }
        }
        catch (Exception exception)
        {
            this.logger?.LogWarning(
                exception,
                "Image metadata {ImageId} was deleted, but binary cleanup failed for {ImagePath}.",
                image.Id,
                image.Path);
        }
    }

    private static async Task SynchronizeAfterDeletionAsync(
        Image image,
        IImageRepository imageRepository,
        IParkRepository parkRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ISearchProjectionWriter searchProjectionWriter,
        CancellationToken consistencyCancellationToken)
    {
        if (image.OwnerType == ImageOwnerType.User && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            IReadOnlyCollection<Image> remainingImages = await imageRepository.GetByOwnerAsync(
                ImageOwnerType.User,
                image.OwnerId,
                ImageCategory.Avatar,
                consistencyCancellationToken);
            Image? replacementCurrent = remainingImages.FirstOrDefault(static candidate => candidate.IsCurrent);

            if (replacementCurrent is null)
            {
                Image? firstRemaining = remainingImages.FirstOrDefault();
                if (firstRemaining is not null)
                {
                    replacementCurrent = await imageRepository.SetCurrentIfUnchangedAsync(
                        firstRemaining.Id,
                        new ImageMutationPrecondition(
                            firstRemaining.OwnerType,
                            firstRemaining.OwnerId,
                            firstRemaining.Category,
                            firstRemaining.IsCurrent),
                        ImageOwnerType.User,
                        image.OwnerId,
                        consistencyCancellationToken,
                        consistencyCancellationToken);
                }
            }
            return;
        }

        if (image.OwnerType == ImageOwnerType.Park && image.Category == ImageCategory.Logo && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            Park? park = await parkRepository.GetByIdAsync(
                image.OwnerId,
                true,
                consistencyCancellationToken);
            if (park is null)
            {
                return;
            }

            Image? currentLogo = await imageRepository.GetCurrentByOwnerAsync(
                ImageOwnerType.Park,
                image.OwnerId,
                ImageCategory.Logo,
                consistencyCancellationToken);
            park.CurrentLogoImageId = currentLogo?.Id;
            await parkRepository.UpdateAsync(park.Id, park, consistencyCancellationToken);
            return;
        }

        if (image.OwnerType == ImageOwnerType.AttractionManufacturer && image.Category == ImageCategory.Logo && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            AttractionManufacturer? manufacturer = await attractionManufacturerRepository.GetByIdAsync(
                image.OwnerId,
                consistencyCancellationToken);
            if (manufacturer is null)
            {
                return;
            }

            Image? currentLogo = await imageRepository.GetCurrentByOwnerAsync(
                ImageOwnerType.AttractionManufacturer,
                image.OwnerId,
                ImageCategory.Logo,
                consistencyCancellationToken);
            manufacturer.CurrentLogoImageId = currentLogo?.Id;
            await attractionManufacturerRepository.UpdateAsync(
                manufacturer.Id,
                manufacturer,
                consistencyCancellationToken);
            await searchProjectionWriter.UpsertAsync(
                SearchProjectionResourceTypes.Manufacturers,
                manufacturer.Id,
                consistencyCancellationToken);
        }
    }

}
