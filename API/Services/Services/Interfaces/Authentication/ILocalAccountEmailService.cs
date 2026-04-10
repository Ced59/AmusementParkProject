using System.Threading;
using System.Threading.Tasks;
using Entities.Model.Users;
using AmusementPark.Application.Ports;

namespace Services.Interfaces.Authentication
{
    public interface ILocalAccountEmailService
    {
        Task SendEmailConfirmationAsync(User user, string confirmationToken, CancellationToken cancellationToken = default);

        Task SendPasswordResetAsync(User user, string resetToken, CancellationToken cancellationToken = default);
    }
}
