using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Handlers;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAccountChangesConcurrently_ShouldNotReplaceTheNewerDocument()
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
            HashedPassword = "existing-hash",
            PasswordResetTokenHash = "reset-token-hash",
            PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            UpdatedAtUtc = expectedUpdatedAtUtc,
        };
        Mock<IRefreshTokenFactory> tokens =
            new Mock<IRefreshTokenFactory>(MockBehavior.Strict);
        tokens.Setup(factory => factory.ComputeHash("reset-token"))
            .Returns("reset-token-hash");
        Mock<IPasswordHasher> passwords =
            new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwords.Setup(hasher => hasher.HashPassword("StrongPassword123!"))
            .Returns("new-password-hash");
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(repository => repository.GetByPasswordResetTokenHashAsync(
                "reset-token-hash",
                CancellationToken.None))
            .ReturnsAsync(user);
        users.Setup(repository => repository.UpdateIfUnchangedAsync(
                user.Id,
                It.Is<User>(value =>
                    value.HashedPassword == "new-password-hash"
                    && value.PasswordResetTokenHash == null),
                expectedUpdatedAtUtc,
                CancellationToken.None))
            .ReturnsAsync((User?)null);
        ResetPasswordCommandHandler handler = new ResetPasswordCommandHandler(
            users.Object,
            tokens.Object,
            passwords.Object);

        ApplicationResult result = await handler.HandleAsync(
            new ResetPasswordCommand(new ResetPasswordRequest
            {
                Token = "reset-token",
                NewPassword = "StrongPassword123!",
                VerifyNewPassword = "StrongPassword123!",
            }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "user.password-reset.failed");
        users.VerifyAll();
        tokens.VerifyAll();
        passwords.VerifyAll();
    }
}
