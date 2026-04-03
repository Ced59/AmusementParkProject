using System.Threading;
using System.Threading.Tasks;
using Entities.Model.Users;
using Services.Models.Authentication;

namespace Services.Interfaces.Authentication
{
    public interface IExternalIdentityProviderService
    {
        ExternalLoginProvider Provider { get; }

        Task<VerifiedExternalIdentity?> VerifyAsync(string token, string? nonce, CancellationToken cancellationToken = default);
    }
}
