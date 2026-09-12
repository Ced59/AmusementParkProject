using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

/// <summary>
/// Décrit la source privée et la politique par défaut propres à un type de publication.
/// </summary>
public interface ISharePublicationSourceDescriptor
{
    SharePublicationType PublicationType { get; }

    ApplicationResult<string> ResolveSourceScopeKey(string ownerUserId, string? sourceId);

    ShareContentPolicy CreateDefaultPolicy();

    ApplicationResult<bool> ValidatePolicyForPublication(ShareContentPolicy contentPolicy);

    Task<ApplicationResult<long>> GetCurrentSourceVersionAsync(
        SharePublicationSourceVersionRequest request,
        CancellationToken cancellationToken);
}
