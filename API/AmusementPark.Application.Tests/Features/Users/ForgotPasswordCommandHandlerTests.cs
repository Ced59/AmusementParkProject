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

public sealed class ForgotPasswordCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAccountChangesConcurrently_ShouldNotReplaceTheNewerDocumentOrSendEmail()
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
            IsActivated = true,
            UpdatedAtUtc = expectedUpdatedAtUtc,
        };
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(repository => repository.GetByEmailAsync(
                "owner@example.com",
                CancellationToken.None))
            .ReturnsAsync(user);
        users.Setup(repository => repository.UpdateIfUnchangedAsync(
                user.Id,
                It.Is<User>(value =>
                    value.PasswordResetTokenHash == "reset-token-hash"),
                expectedUpdatedAtUtc,
                CancellationToken.None))
            .ReturnsAsync((User?)null);
        Mock<IRefreshTokenFactory> tokens =
            new Mock<IRefreshTokenFactory>(MockBehavior.Strict);
        tokens.Setup(factory => factory.Generate()).Returns("reset-token");
        tokens.Setup(factory => factory.ComputeHash("reset-token"))
            .Returns("reset-token-hash");
        Mock<IUserAuthenticationSettings> settings =
            new Mock<IUserAuthenticationSettings>(MockBehavior.Strict);
        settings.SetupGet(value => value.PasswordResetTokenExpirationMinutes)
            .Returns(30);
        Mock<ILocalAccountEmailService> emails =
            new Mock<ILocalAccountEmailService>(MockBehavior.Strict);
        ForgotPasswordCommandHandler handler = new ForgotPasswordCommandHandler(
            users.Object,
            tokens.Object,
            emails.Object,
            settings.Object);

        ApplicationResult result = await handler.HandleAsync(
            new ForgotPasswordCommand(new ForgotPasswordRequest
            {
                Email = "owner@example.com",
            }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "user.password-reset.email.failed");
        users.VerifyAll();
        tokens.VerifyAll();
        settings.VerifyAll();
        emails.VerifyNoOtherCalls();
    }
}
