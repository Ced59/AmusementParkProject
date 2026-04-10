using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de confirmation d'email.
/// </summary>
public sealed class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenFactory refreshTokenFactory;

    public ConfirmEmailCommandHandler(IUserRepository userRepository, IRefreshTokenFactory refreshTokenFactory)
    {
        this.userRepository = userRepository;
        this.refreshTokenFactory = refreshTokenFactory;
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

        user.IsActivated = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        user.EmailConfirmationTokenHash = null;
        user.EmailConfirmationTokenExpiresAtUtc = null;
        user.EmailConfirmationSentAtUtc = null;

        User? updatedUser = await this.userRepository.UpdateAsync(user.Id, user, cancellationToken);
        if (updatedUser is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserUpdateFailed());
        }

        return ApplicationResult<User>.Success(updatedUser);
    }
}
