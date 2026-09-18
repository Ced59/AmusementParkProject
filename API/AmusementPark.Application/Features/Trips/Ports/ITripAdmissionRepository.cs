using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripAdmissionRepository
{
    Task<TripInvitation?> GetInvitationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<TripAdmissionFenceWriteResult> PrepareFenceAsync(
        TripPlanId tripPlanId,
        TripInvitationId invitationId,
        string candidateUserId,
        string operationId,
        CancellationToken cancellationToken);

    Task<TripAdmissionWriteOutcome> ReserveInvitationAsync(
        TripInvitation invitation,
        string candidateUserId,
        string operationId,
        string operationKeyHash,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken);

    Task<TripAdmissionWriteOutcome> ArmFenceAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken);

    Task<TripAdmissionWriteOutcome> ApplyProvisionalMemberAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        TripDelegatedRole role,
        CancellationToken cancellationToken);

    Task<TripAdmissionWriteOutcome> MarkInvitationAcceptedAsync(
        TripInvitationId invitationId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken);

    Task<TripAdmissionWriteOutcome> EstablishMemberAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task MarkInvitationAdmissionCompletedAsync(
        TripInvitationId invitationId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken);

    Task<TripAdmissionWriteOutcome> CancelFenceAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken);

    Task<TripAdmissionWriteOutcome> CancelExpiredFenceAsync(
        TripPlanId tripPlanId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken);

    Task CancelInvitationAcceptanceAsync(
        TripInvitationId invitationId,
        TripMemberAdmissionFence fence,
        CancellationToken cancellationToken);

    Task<TripAdmissionWriteOutcome> DeclineInvitationAsync(
        TripInvitation invitation,
        string candidateUserId,
        string operationKeyHash,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TripInvitation>> ListPendingAcceptancesAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TripPlan>> ListPendingAdmissionFencesAsync(
        int limit,
        CancellationToken cancellationToken);
}
