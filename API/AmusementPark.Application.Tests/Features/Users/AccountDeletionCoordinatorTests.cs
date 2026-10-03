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
using AmusementPark.Application.Features.Users.Models;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Services;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class AccountDeletionCoordinatorTests
{
    [Fact]
    public async Task DeleteAsync_ShouldDeleteIdentityOnlyAfterAllProductData()
    {
        AccountDeletionCoordinatorFixture fixture = new AccountDeletionCoordinatorFixture();
        MockSequence sequence = new MockSequence();
        fixture.Users.InSequence(sequence)
            .Setup(value => value.GetByIdAsync("user-1", CancellationToken.None))
            .ReturnsAsync(fixture.User);
        fixture.Locks.InSequence(sequence)
            .Setup(value => value.HandleAsync(
                new LockUserCommand("user-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<User>.Success(fixture.User));
        fixture.RefreshTokens.InSequence(sequence)
            .Setup(value => value.RevokeAllAsync(
                "user-1",
                "AccountDeletion",
                CancellationToken.None))
            .ReturnsAsync(1);
        fixture.Ratings.InSequence(sequence)
            .Setup(value => value.GetUserRatingsAsync(
                "user-1",
                1,
                100,
                null,
                CancellationToken.None))
            .ReturnsAsync(fixture.EmptyRatings());
        fixture.Shares.InSequence(sequence)
            .Setup(value => value.DeleteAsync("user-1", CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ShareAccountDeletionResult>.Success(
                new ShareAccountDeletionResult(0, 0, 0, 0)));
        fixture.Watchlists.InSequence(sequence)
            .Setup(value => value.DeleteAsync("user-1", CancellationToken.None))
            .ReturnsAsync(new WatchlistAccountDeletionResult(0, 0, 0, 0, 0, 0, 0));
        fixture.OwnedImages.InSequence(sequence)
            .Setup(value => value.ListAsync("user-1", CancellationToken.None))
            .ReturnsAsync(Array.Empty<AccountOwnedImage>());
        fixture.Data.InSequence(sequence)
            .Setup(value => value.PurgeAsync("user-1", CancellationToken.None))
            .ReturnsAsync(new AccountDataDeletionResult(8));
        fixture.RefreshTokens.InSequence(sequence)
            .Setup(value => value.DeleteAllAsync("user-1", CancellationToken.None))
            .ReturnsAsync(1);
        fixture.Users.InSequence(sequence)
            .Setup(value => value.DeleteAsync("user-1", CancellationToken.None))
            .ReturnsAsync(true);

        await fixture.CreateCoordinator().DeleteAsync("user-1", CancellationToken.None);

        fixture.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_WhenOwnedImageBinaryFails_ShouldRetryBeforeDeletingMetadata()
    {
        AccountDeletionCoordinatorFixture fixture = new AccountDeletionCoordinatorFixture();
        fixture.SetupBeforeImages();
        fixture.OwnedImages.Setup(value => value.ListAsync(
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(new[] { new AccountOwnedImage("image-1", "users/user-1/avatar") });
        fixture.ImageBinaries.Setup(value => value.DeleteAsync(
                "users/user-1/avatar",
                CancellationToken.None))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.CreateCoordinator().DeleteAsync("user-1", CancellationToken.None));

        fixture.Images.Verify(
            value => value.DeleteForAccountDeletionAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Data.Verify(
            value => value.PurgeAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Users.Verify(
            value => value.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

}
