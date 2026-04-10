namespace AmusementPark.Application.Features.Users.Ports;

/// <summary>
/// Paramètres fonctionnels nécessaires aux cas d'usage utilisateurs.
/// </summary>
public interface IUserAuthenticationSettings
{
    int EmailConfirmationTokenExpirationHours { get; }

    int PasswordResetTokenExpirationMinutes { get; }

    int TokenRefreshLimitMinutes { get; }

    string FrontendBaseUrl { get; }
}
