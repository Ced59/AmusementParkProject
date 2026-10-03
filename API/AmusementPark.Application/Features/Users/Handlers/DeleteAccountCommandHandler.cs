using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Models;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Services;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

public sealed class DeleteAccountCommandHandler
    : ICommandHandler<DeleteAccountCommand, ApplicationResult>
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IAccountDeletionOperationRepository operationRepository;
    private readonly ICommandHandler<LockUserCommand, ApplicationResult<User>> lockUserHandler;
    private readonly AccountDeletionScheduler scheduler;

    public DeleteAccountCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IAccountDeletionOperationRepository operationRepository,
        ICommandHandler<LockUserCommand, ApplicationResult<User>> lockUserHandler,
        AccountDeletionScheduler scheduler)
    {
        this.userRepository = userRepository;
        this.refreshTokenRepository = refreshTokenRepository;
        this.passwordHasher = passwordHasher;
        this.operationRepository = operationRepository;
        this.lockUserHandler = lockUserHandler;
        this.scheduler = scheduler;
    }

    public async Task<ApplicationResult> HandleAsync(
        DeleteAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult.Failure(
                UserApplicationErrors.AccountDeletionConfirmationInvalid());
        }

        User? user = await this.userRepository.GetByIdAsync(
            command.UserId,
            cancellationToken);
        if (user is null)
        {
            return ApplicationResult.Failure(UserApplicationErrors.UserNotExists());
        }

        string confirmationEmail = UserRules.NormalizeEmail(
            command.Request.ConfirmationEmail) ?? string.Empty;
        if (!string.Equals(
                confirmationEmail,
                UserRules.NormalizeEmail(user.Email),
                StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult.Failure(
                UserApplicationErrors.AccountDeletionConfirmationInvalid());
        }

        if (!string.IsNullOrWhiteSpace(user.HashedPassword)
            && !this.passwordHasher.VerifyPassword(
                command.Request.CurrentPassword,
                user.HashedPassword))
        {
            return ApplicationResult.Failure(UserApplicationErrors.IncorrectPassword());
        }

        AccountDeletionOperation operation =
            await this.operationRepository.CreateOrGetAsync(
                user.Id,
                DateTime.UtcNow,
                cancellationToken);
        _ = await this.scheduler.ScheduleAsync(operation.Id, cancellationToken);
        _ = await this.lockUserHandler.HandleAsync(
            new LockUserCommand(user.Id),
            cancellationToken);
        _ = await this.refreshTokenRepository.RevokeAllAsync(
            user.Id,
            "AccountDeletionRequested",
            cancellationToken);
        return ApplicationResult.Success();
    }
}
