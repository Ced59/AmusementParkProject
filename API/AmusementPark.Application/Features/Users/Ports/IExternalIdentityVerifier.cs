using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Ports;

/// <summary>
/// Port applicatif de vérification d'identités externes.
/// </summary>
public interface IExternalIdentityVerifier
{
    bool Supports(ExternalLoginProvider provider);

    Task<VerifiedExternalIdentity?> VerifyAsync(ExternalLoginProvider provider, string token, string? nonce, CancellationToken cancellationToken = default);
}
