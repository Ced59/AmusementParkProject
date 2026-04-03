using System.Threading;
using System.Threading.Tasks;

namespace Services.Interfaces.Authentication
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string plainTextBody, CancellationToken cancellationToken = default);
    }
}
