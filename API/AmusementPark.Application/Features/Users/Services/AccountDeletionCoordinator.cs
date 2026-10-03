using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Users.Models;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Services;

public sealed class AccountDeletionCoordinator
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly AccountRatingDeletionService ratingDeletionService;
    private readonly IShareAccountDeletionService shareDeletionService;
    private readonly IWatchlistAccountDeletionService watchlistDeletionService;
    private readonly IAccountDataDeletionStore dataDeletionStore;
    private readonly IAccountOwnedImageReader ownedImageReader;
    private readonly IImageRepository imageRepository;
    private readonly IImageBinaryStorage imageBinaryStorage;
    private readonly ICommandHandler<LockUserCommand, ApplicationResult<User>> lockUserHandler;

    public AccountDeletionCoordinator(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        AccountRatingDeletionService ratingDeletionService,
        IShareAccountDeletionService shareDeletionService,
        IWatchlistAccountDeletionService watchlistDeletionService,
        IAccountDataDeletionStore dataDeletionStore,
        IAccountOwnedImageReader ownedImageReader,
        IImageRepository imageRepository,
        IImageBinaryStorage imageBinaryStorage,
        ICommandHandler<LockUserCommand, ApplicationResult<User>> lockUserHandler)
    {
        this.userRepository = userRepository;
        this.refreshTokenRepository = refreshTokenRepository;
        this.ratingDeletionService = ratingDeletionService;
        this.shareDeletionService = shareDeletionService;
        this.watchlistDeletionService = watchlistDeletionService;
        this.dataDeletionStore = dataDeletionStore;
        this.ownedImageReader = ownedImageReader;
        this.imageRepository = imageRepository;
        this.imageBinaryStorage = imageBinaryStorage;
        this.lockUserHandler = lockUserHandler;
    }

    public async Task DeleteAsync(string userId, CancellationToken cancellationToken)
    {
        if (await this.userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            return;
        }

        ApplicationResult<User> lockResult =
            await this.lockUserHandler.HandleAsync(
                new LockUserCommand(userId),
                cancellationToken);
        if (!lockResult.IsSuccess)
        {
            throw new InvalidOperationException("The account could not be locked for deletion.");
        }
        _ = await this.refreshTokenRepository.RevokeAllAsync(
            userId,
            "AccountDeletion",
            cancellationToken);

        _ = await this.ratingDeletionService.DeleteAsync(userId, cancellationToken);

        ApplicationResult<ShareAccountDeletionResult> shareResult =
            await this.shareDeletionService.DeleteAsync(userId, cancellationToken);
        if (!shareResult.IsSuccess)
        {
            throw new InvalidOperationException(
                "The account sharing perimeter could not be deleted safely.");
        }

        _ = await this.watchlistDeletionService.DeleteAsync(userId, cancellationToken);
        await this.DeleteOwnedImagesAsync(userId, cancellationToken);
        _ = await this.dataDeletionStore.PurgeAsync(userId, cancellationToken);
        _ = await this.refreshTokenRepository.DeleteAllAsync(userId, cancellationToken);

        bool deleted = await this.userRepository.DeleteAsync(userId, cancellationToken);
        if (!deleted
            && await this.userRepository.GetByIdAsync(userId, cancellationToken) is not null)
        {
            throw new InvalidOperationException("The account identity could not be deleted.");
        }
    }

    private async Task DeleteOwnedImagesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AccountOwnedImage> images =
            await this.ownedImageReader.ListAsync(userId, cancellationToken);
        foreach (AccountOwnedImage image in images)
        {
            if (!string.IsNullOrWhiteSpace(image.Path))
            {
                bool binaryDeleted = await this.imageBinaryStorage.DeleteAsync(
                    image.Path,
                    cancellationToken);
                if (!binaryDeleted)
                {
                    throw new InvalidOperationException(
                        "An owned image binary could not be deleted.");
                }
            }

            bool metadataDeleted = await this.imageRepository
                .DeleteForAccountDeletionAsync(image.Id, cancellationToken);
            if (!metadataDeleted)
            {
                throw new InvalidOperationException(
                    "Owned image metadata could not be deleted.");
            }
        }
    }
}
