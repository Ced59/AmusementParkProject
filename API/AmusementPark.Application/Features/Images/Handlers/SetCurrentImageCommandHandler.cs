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

/// <summary>
/// Handler de promotion d'une image en image courante.
/// </summary>
public sealed class SetCurrentImageCommandHandler : ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>
{
    private readonly IImageRepository imageRepository;
    private readonly IParkRepository parkRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IUserRepository userRepository;
    private readonly IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard;
    private readonly IPublicSeoUpdateNotifier? publicSeoUpdateNotifier;

    public SetCurrentImageCommandHandler(
        IImageRepository imageRepository,
        IParkRepository parkRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IUserRepository userRepository,
        IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard,
        IPublicSeoUpdateNotifier? publicSeoUpdateNotifier = null)
    {
        this.imageRepository = imageRepository;
        this.parkRepository = parkRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.userRepository = userRepository;
        this.shareSourceRevisionGuard = shareSourceRevisionGuard;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
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

            if (ManagedCommentImageMutationGuard.IsManagedScope(image))
            {
                return ApplicationResult<Image>.Failure(
                    ImageApplicationErrors.CommentImageLifecycleManaged());
            }

            if (image.OwnerType == ImageOwnerType.None || string.IsNullOrWhiteSpace(image.OwnerId))
            {
                return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotLinkedToOwner());
            }

            IReadOnlyCollection<string> avatarOwnerUserIds =
                UserAvatarShareSourceMutation.ResolveImpactedOwnerUserIds(
                    image,
                    image.OwnerType,
                    image.OwnerId,
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
            Image? updated;
            try
            {
                avatarSourceChanged = avatarOwnerUserIds.Count > 0;
                updated = await this.imageRepository.SetCurrentIfUnchangedAsync(
                    image.Id,
                    new ImageMutationPrecondition(
                        image.OwnerType,
                        image.OwnerId,
                        image.Category,
                        image.IsCurrent),
                    image.OwnerType,
                    image.OwnerId,
                    cancellationToken,
                    consistencyCancellation.Token);
                if (updated is null)
                {
                    return ApplicationResult<Image>.Failure(ImageApplicationErrors.ErrorSettingCurrentImage());
                }

                await UserAvatarShareSourceMutation.SynchronizeAsync(
                    avatarOwnerUserIds,
                    this.imageRepository,
                    this.userRepository,
                    consistencyCancellation.Token);
                await SynchronizeOwnerAsync(
                    updated,
                    this.parkRepository,
                    this.attractionManufacturerRepository,
                    this.searchProjectionWriter,
                    consistencyCancellation.Token);
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
                new[] { updated },
                cancellationToken);
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
