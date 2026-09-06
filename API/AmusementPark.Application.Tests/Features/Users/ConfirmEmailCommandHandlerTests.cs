using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Handlers;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class ConfirmEmailCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAccountChangesConcurrently_ShouldAdvanceLeaseConservatively()
    {
        DateTime expectedUpdatedAtUtc = new DateTime(
            2026,
            9,
            6,
            16,
            30,
            0,
            DateTimeKind.Utc);
        User user = new User
        {
            Id = "user-1",
            Email = "owner@example.com",
            IsActivated = false,
            EmailConfirmationTokenHash = "confirmation-token-hash",
            EmailConfirmationTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            UpdatedAtUtc = expectedUpdatedAtUtc,
        };
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            "personal-ranking:user-1");
        Mock<IRefreshTokenFactory> tokens =
            new Mock<IRefreshTokenFactory>(MockBehavior.Strict);
        tokens.Setup(factory => factory.ComputeHash("confirmation-token"))
            .Returns("confirmation-token-hash");
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(repository => repository.GetByEmailConfirmationTokenHashAsync(
                "confirmation-token-hash",
                CancellationToken.None))
            .ReturnsAsync(user);
        users.Setup(repository => repository.UpdateIfUnchangedAsync(
                user.Id,
                It.Is<User>(value => value.IsActivated),
                expectedUpdatedAtUtc,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync((User?)null);
        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        revisions.Setup(guard => guard.BeginMutationAsync(
                user.Id,
                CancellationToken.None))
            .ReturnsAsync(lease);
        revisions.Setup(guard => guard.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        ConfirmEmailCommandHandler handler = new ConfirmEmailCommandHandler(
            users.Object,
            tokens.Object,
            revisions.Object);

        ApplicationResult<User> result = await handler.HandleAsync(
            new ConfirmEmailCommand("confirmation-token"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "user.update.failed");
        users.VerifyAll();
        tokens.VerifyAll();
        revisions.VerifyAll();
    }
}
