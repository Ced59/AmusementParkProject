using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IProfileComparisonInvitationRepository
{
    Task<ProfileComparisonInvitation?> GetByTokenAsync(
        ShareToken token,
        CancellationToken cancellationToken);

    Task<ProfileComparisonInvitationWriteOutcome> CreateAsync(
        ProfileComparisonInvitation invitation,
        CancellationToken cancellationToken);

    Task<ProfileComparisonInvitationWriteOutcome> ReplaceAsync(
        ProfileComparisonInvitation invitation,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<bool> DeleteAcceptedAsync(
        ProfileComparisonInvitationId invitationId,
        long expectedVersion,
        CancellationToken cancellationToken);
}
