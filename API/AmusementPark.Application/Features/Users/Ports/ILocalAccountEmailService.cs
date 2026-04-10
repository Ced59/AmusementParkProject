using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Ports;

/// <summary>
/// Port applicatif d'envoi des emails locaux liés au compte utilisateur.
/// </summary>
public interface ILocalAccountEmailService
{
    Task SendEmailConfirmationAsync(User user, string confirmationToken, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(User user, string resetToken, CancellationToken cancellationToken = default);
}
