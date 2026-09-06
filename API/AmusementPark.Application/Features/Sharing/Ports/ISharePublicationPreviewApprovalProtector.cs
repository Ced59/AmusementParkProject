using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

/// <summary>
/// Lie une approbation opaque au propriétaire, à la source et à la politique réellement prévisualisée.
/// </summary>
public interface ISharePublicationPreviewApprovalProtector
{
    string CreateToken(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        long sourceVersion,
        ShareContentPolicy contentPolicy);

    bool IsValid(
        string approvalToken,
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        long sourceVersion,
        ShareContentPolicy contentPolicy);
}
