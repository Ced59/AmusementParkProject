using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Persists a validated preferred language without replacing the rest of the profile.
/// </summary>
public sealed class UpdatePreferredLanguageCommandHandler
    : ICommandHandler<UpdatePreferredLanguageCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;

    public UpdatePreferredLanguageCommandHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<User>> HandleAsync(
        UpdatePreferredLanguageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        if (!PreferredLanguagePolicy.TryNormalize(command.PreferredLanguage, out string normalizedLanguage))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.InvalidPreferredLanguage());
        }

        try
        {
            User? updatedUser = await this.userRepository.UpdatePreferredLanguageAsync(
                command.UserId.Trim(),
                normalizedLanguage,
                cancellationToken);
            if (updatedUser is null)
            {
                return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
            }

            return ApplicationResult<User>.Success(updatedUser);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserUpdateFailed());
        }
    }
}
