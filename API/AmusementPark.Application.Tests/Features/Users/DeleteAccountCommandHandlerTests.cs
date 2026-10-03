using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Handlers;
using AmusementPark.Application.Features.Users.Models;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Services;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class DeleteAccountCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenConfirmationEmailDoesNotMatch_ShouldRejectWithoutScheduling()
    {
        User user = CreateUser("hash");
        Mock<IUserRepository> users = CreateUserRepository(user);
        Mock<IPasswordHasher> passwords = new Mock<IPasswordHasher>(MockBehavior.Strict);
        DeleteAccountCommandHandler handler = CreateHandler(
            users,
            passwords,
            out Mock<IAccountDeletionOperationRepository> operations,
            out Mock<IDurableBackgroundJobRepository> jobs,
            out Mock<IRefreshTokenRepository> refreshTokens,
            out Mock<ICommandHandler<LockUserCommand, ApplicationResult<User>>> locks);

        ApplicationResult result = await handler.HandleAsync(
            new DeleteAccountCommand(
                user.Id,
                new DeleteAccountRequest("another@example.com", "password")));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "user.account-deletion.confirmation-invalid");
        operations.VerifyNoOtherCalls();
        jobs.VerifyNoOtherCalls();
        refreshTokens.VerifyNoOtherCalls();
        locks.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenLocalPasswordIsIncorrect_ShouldRejectWithoutScheduling()
    {
        User user = CreateUser("hash");
        Mock<IUserRepository> users = CreateUserRepository(user);
        Mock<IPasswordHasher> passwords = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwords.Setup(value => value.VerifyPassword("wrong", "hash"))
            .Returns(false);
        DeleteAccountCommandHandler handler = CreateHandler(
            users,
            passwords,
            out Mock<IAccountDeletionOperationRepository> operations,
            out Mock<IDurableBackgroundJobRepository> jobs,
            out Mock<IRefreshTokenRepository> refreshTokens,
            out Mock<ICommandHandler<LockUserCommand, ApplicationResult<User>>> locks);

        ApplicationResult result = await handler.HandleAsync(
            new DeleteAccountCommand(
                user.Id,
                new DeleteAccountRequest(" member@example.com ", "wrong")));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "user.password.incorrect");
        passwords.VerifyAll();
        operations.VerifyNoOtherCalls();
        jobs.VerifyNoOtherCalls();
        refreshTokens.VerifyNoOtherCalls();
        locks.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ForExternalAccount_ShouldScheduleThenCutOffAccess()
    {
        User user = CreateUser(null);
        Mock<IUserRepository> users = CreateUserRepository(user);
        Mock<IPasswordHasher> passwords = new Mock<IPasswordHasher>(MockBehavior.Strict);
        DeleteAccountCommandHandler handler = CreateHandler(
            users,
            passwords,
            out Mock<IAccountDeletionOperationRepository> operations,
            out Mock<IDurableBackgroundJobRepository> jobs,
            out Mock<IRefreshTokenRepository> refreshTokens,
            out Mock<ICommandHandler<LockUserCommand, ApplicationResult<User>>> locks);
        AccountDeletionOperation operation = new AccountDeletionOperation(
            "operation-1",
            user.Id,
            DateTime.UtcNow);
        operations.Setup(value => value.CreateOrGetAsync(
                user.Id,
                It.Is<DateTime>(date => date.Kind == DateTimeKind.Utc),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(operation);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == AccountDeletionJob.Kind
                    && request.IdempotencyKey == "account-deletion:operation-1"
                    && request.Payload.GetProperty("OperationId").GetString() == "operation-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DurableBackgroundJob)null!);
        locks.Setup(value => value.HandleAsync(
                new LockUserCommand(user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<User>.Failure(
                ApplicationError.Technical("user.lock.failed", "Lock failed.")));
        refreshTokens.Setup(value => value.RevokeAllAsync(
                user.Id,
                "AccountDeletionRequested",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        ApplicationResult result = await handler.HandleAsync(
            new DeleteAccountCommand(
                user.Id,
                new DeleteAccountRequest("MEMBER@EXAMPLE.COM", string.Empty)));

        Assert.True(result.IsSuccess);
        operations.VerifyAll();
        jobs.VerifyAll();
        locks.VerifyAll();
        refreshTokens.VerifyAll();
        passwords.VerifyNoOtherCalls();
    }

    private static DeleteAccountCommandHandler CreateHandler(
        Mock<IUserRepository> users,
        Mock<IPasswordHasher> passwords,
        out Mock<IAccountDeletionOperationRepository> operations,
        out Mock<IDurableBackgroundJobRepository> jobs,
        out Mock<IRefreshTokenRepository> refreshTokens,
        out Mock<ICommandHandler<LockUserCommand, ApplicationResult<User>>> locks)
    {
        operations = new Mock<IAccountDeletionOperationRepository>(MockBehavior.Strict);
        jobs = new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        refreshTokens = new Mock<IRefreshTokenRepository>(MockBehavior.Strict);
        locks = new Mock<ICommandHandler<LockUserCommand, ApplicationResult<User>>>(
            MockBehavior.Strict);
        return new DeleteAccountCommandHandler(
            users.Object,
            refreshTokens.Object,
            passwords.Object,
            operations.Object,
            locks.Object,
            new AccountDeletionScheduler(jobs.Object));
    }

    private static Mock<IUserRepository> CreateUserRepository(User user)
    {
        Mock<IUserRepository> repository = new Mock<IUserRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        return repository;
    }

    private static User CreateUser(string? hashedPassword)
    {
        return new User
        {
            Id = "user-1",
            Email = "member@example.com",
            HashedPassword = hashedPassword,
            IsActivated = true,
            Roles = new List<Role> { Role.User },
        };
    }
}
