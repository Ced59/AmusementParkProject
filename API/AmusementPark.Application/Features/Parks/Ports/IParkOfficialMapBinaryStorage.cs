using AmusementPark.Application.Common.Contracts;

namespace AmusementPark.Application.Features.Parks.Ports;

/// <summary>
/// Port de stockage des documents de carte officielle, séparé du pipeline d'images.
/// </summary>
public interface IParkOfficialMapBinaryStorage
{
    Task SaveAsync(
        string storageKey,
        FilePayload file,
        string canonicalContentType,
        CancellationToken cancellationToken);

    Task<Stream?> GetAsync(string storageKey, CancellationToken cancellationToken);
}
