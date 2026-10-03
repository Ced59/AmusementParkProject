using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Services;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Users;
using Moq;

namespace AmusementPark.Application.Tests.Features.Users;

internal sealed class AccountDeletionCoordinatorFixture
{
    public AccountDeletionCoordinatorFixture()
    {
        this.User = new User
        {
            Id = "user-1",
            Email = "member@example.com",
            IsActivated = true,
            Roles = new List<Role> { Role.User },
        };
    }

    public User User { get; }

    public Mock<IUserRepository> Users { get; } =
        new Mock<IUserRepository>(MockBehavior.Strict);

    public Mock<IRefreshTokenRepository> RefreshTokens { get; } =
        new Mock<IRefreshTokenRepository>(MockBehavior.Strict);

    public Mock<IRatingRepository> Ratings { get; } =
        new Mock<IRatingRepository>(MockBehavior.Strict);

    public Mock<ICommandHandler<DeleteUserRatingCommand, ApplicationResult<RatingSummaryResult>>> RatingHandler { get; } =
        new Mock<ICommandHandler<DeleteUserRatingCommand, ApplicationResult<RatingSummaryResult>>>(
            MockBehavior.Strict);

    public Mock<IShareAccountDeletionService> Shares { get; } =
        new Mock<IShareAccountDeletionService>(MockBehavior.Strict);

    public Mock<IWatchlistAccountDeletionService> Watchlists { get; } =
        new Mock<IWatchlistAccountDeletionService>(MockBehavior.Strict);

    public Mock<IAccountDataDeletionStore> Data { get; } =
        new Mock<IAccountDataDeletionStore>(MockBehavior.Strict);

    public Mock<IAccountOwnedImageReader> OwnedImages { get; } =
        new Mock<IAccountOwnedImageReader>(MockBehavior.Strict);

    public Mock<IImageRepository> Images { get; } =
        new Mock<IImageRepository>(MockBehavior.Strict);

    public Mock<IImageBinaryStorage> ImageBinaries { get; } =
        new Mock<IImageBinaryStorage>(MockBehavior.Strict);

    public Mock<ICommandHandler<LockUserCommand, ApplicationResult<User>>> Locks { get; } =
        new Mock<ICommandHandler<LockUserCommand, ApplicationResult<User>>>(
            MockBehavior.Strict);

    public AccountDeletionCoordinator CreateCoordinator()
    {
        return new AccountDeletionCoordinator(
            this.Users.Object,
            this.RefreshTokens.Object,
            new AccountRatingDeletionService(
                this.Ratings.Object,
                this.RatingHandler.Object),
            this.Shares.Object,
            this.Watchlists.Object,
            this.Data.Object,
            this.OwnedImages.Object,
            this.Images.Object,
            this.ImageBinaries.Object,
            this.Locks.Object);
    }

    public PagedResult<UserRatingListItemResult> EmptyRatings()
    {
        return new PagedResult<UserRatingListItemResult>(
            Array.Empty<UserRatingListItemResult>(),
            1,
            100,
            0);
    }

    public void SetupBeforeImages()
    {
        this.Users.Setup(value => value.GetByIdAsync(
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(this.User);
        this.Locks.Setup(value => value.HandleAsync(
                new LockUserCommand("user-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<User>.Success(this.User));
        this.RefreshTokens.Setup(value => value.RevokeAllAsync(
                "user-1",
                "AccountDeletion",
                CancellationToken.None))
            .ReturnsAsync(1);
        this.Ratings.Setup(value => value.GetUserRatingsAsync(
                "user-1",
                1,
                100,
                null,
                CancellationToken.None))
            .ReturnsAsync(this.EmptyRatings());
        this.Shares.Setup(value => value.DeleteAsync(
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ShareAccountDeletionResult>.Success(
                new ShareAccountDeletionResult(0, 0, 0, 0)));
        this.Watchlists.Setup(value => value.DeleteAsync(
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(new WatchlistAccountDeletionResult(0, 0, 0, 0, 0, 0, 0));
    }

    public void VerifyAll()
    {
        this.Users.VerifyAll();
        this.RefreshTokens.VerifyAll();
        this.Ratings.VerifyAll();
        this.Shares.VerifyAll();
        this.Watchlists.VerifyAll();
        this.Data.VerifyAll();
        this.OwnedImages.VerifyAll();
        this.Locks.VerifyAll();
    }
}
