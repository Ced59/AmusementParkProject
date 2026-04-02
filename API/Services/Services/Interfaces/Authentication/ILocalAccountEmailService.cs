using Entities.Model.Users;

namespace Services.Interfaces.Authentication
{
    public interface ILocalAccountEmailService
    {
        Task SendEmailConfirmationAsync(User user, string confirmationToken, CancellationToken cancellationToken = default);

        Task SendPasswordResetAsync(User user, string resetToken, CancellationToken cancellationToken = default);
    }
}
