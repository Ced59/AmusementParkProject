using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de confirmation d'email.
/// </summary>
public sealed class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard;

    public ConfirmEmailCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenFactory refreshTokenFactory,
        IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard)
    {
        this.userRepository = userRepository;
        this.refreshTokenFactory = refreshTokenFactory;
        this.shareSourceRevisionGuard = shareSourceRevisionGuard;
    }

    public async Task<ApplicationResult<User>> HandleAsync(ConfirmEmailCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.EmailConfirmationTokenInvalid());
        }

        string tokenHash = this.refreshTokenFactory.ComputeHash(command.Token);
        User? user = await this.userRepository.GetByEmailConfirmationTokenHashAsync(tokenHash, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.EmailConfirmationTokenInvalid());
        }

        if (user.IsActivated)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.AccountAlreadyActivated());
        }

        if (!user.EmailConfirmationTokenExpiresAtUtc.HasValue || user.EmailConfirmationTokenExpiresAtUtc.Value < DateTime.UtcNow)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.EmailConfirmationTokenExpired());
        }

        ShareSourceMutationLease mutationLease =
            await this.shareSourceRevisionGuard.BeginMutationAsync(user.Id, cancellationToken);
        using CancellationTokenSource mutationCancellation =
            ShareSourceMutationCancellation.CreateLinkedSource(
                cancellationToken,
                mutationLease);
        DateTime expectedUpdatedAtUtc = user.UpdatedAtUtc;
        user.IsActivated = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        user.EmailConfirmationTokenHash = null;
        user.EmailConfirmationTokenExpiresAtUtc = null;
        user.EmailConfirmationSentAtUtc = null;

        User? updatedUser = null;
        try
        {
            updatedUser = await this.userRepository.UpdateIfUnchangedAsync(
                user.Id,
                user,
                expectedUpdatedAtUtc,
                mutationCancellation.Token);
        }
        finally
        {
            await this.shareSourceRevisionGuard.CompleteMutationAsync(
                mutationLease,
                updatedUser is not null,
                CancellationToken.None);
        }
        if (updatedUser is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserUpdateFailed());
        }

        return ApplicationResult<User>.Success(updatedUser);
    }
}
