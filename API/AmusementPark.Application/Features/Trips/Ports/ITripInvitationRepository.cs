using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripInvitationRepository
{
    Task<TripInvitationCreationRecord?> ResolveCreationAsync(
        TripPlanId tripPlanId,
        TripMemberId inviterMemberId,
        string operationKeyHash,
        CancellationToken cancellationToken);

    Task<TripInvitationCreationWriteResult> CreateAsync(
        TripInvitation invitation,
        TripChildMutationLease lease,
        string operationKeyHash,
        string requestHash,
        string sealedToken,
        string sealedTokenKeyVersion,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TripInvitation>> ListActiveAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<TripInvitation?> GetOwnedAsync(
        TripPlanId tripPlanId,
        TripInvitationId invitationId,
        CancellationToken cancellationToken);

    Task<TripInvitation?> GetPublicByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<TripInvitationWriteOutcome> RevokeAsync(
        TripInvitation invitation,
        long expectedVersion,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task<int> ExpireElapsedAsync(int limit, CancellationToken cancellationToken);

    Task PurgeAsync(TripPlanId tripPlanId, CancellationToken cancellationToken);
}
